using ClarityBelongs.Web.Domain;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ClarityBelongs.Web.Observation;

public sealed class DnsRecordObservationAdapter(HttpClient http) : IObservationAdapter
{
    public string AdapterType => AdapterTypes.DnsRecord;

    public async Task<ObservationResult> ObserveAsync(
        Target target,
        SourceDefinition source,
        CancellationToken cancellationToken = default)
    {
        var host = DnsObservationAdapter
            .NormalizeHost(target.PrimaryUri)
            .ToLowerInvariant();
        var recordType = GetRecordType(source.ConfigurationJson);

        if (string.IsNullOrWhiteSpace(host))
        {
            return Failure(
                "invalid_host",
                "Enter a valid public hostname.");
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://cloudflare-dns.com/dns-query?name={Uri.EscapeDataString(host)}&type={Uri.EscapeDataString(recordType)}");
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/dns-json"));
            request.Headers.UserAgent.ParseAdd("ClarityBelongs/0.6");

            using var response = await http.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Failure(
                    "dns_http",
                    $"DNS lookup returned HTTP {(int)response.StatusCode}.");
            }

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            var status = doc.RootElement.TryGetProperty("Status", out var statusNode)
                ? statusNode.GetInt32()
                : -1;
            var answers = new List<string>();

            if (doc.RootElement.TryGetProperty("Answer", out var answerNode))
            {
                foreach (var answer in answerNode.EnumerateArray())
                {
                    if (!answer.TryGetProperty("data", out var data))
                        continue;

                    var value = data.GetString();

                    if (!string.IsNullOrWhiteSpace(value))
                        answers.Add(value.Trim());
                }
            }

            var normalizedAnswers = answers
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var normalized = JsonSerializer.Serialize(new
            {
                host,
                recordType,
                status,
                answers = normalizedAnswers
            });
            var success = status == 0;

            return new ObservationResult(
                success,
                success ? "Healthy" : "Warning",
                "application/json",
                normalized,
                normalizedAnswers.Length > 0
                    ? $"{recordType}: {string.Join(", ", normalizedAnswers)}"
                    : $"No {recordType} records returned.",
                ErrorCode: success ? null : "dns_status",
                ErrorMessage: success ? null : $"DNS status {status}");
        }
        catch (Exception ex) when (
            ex is HttpRequestException
                or JsonException
                or TaskCanceledException)
        {
            return Failure(
                "dns_record_error",
                ex.Message);
        }
    }

    private static string GetRecordType(string? configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
            return "A";

        try
        {
            using var doc = JsonDocument.Parse(configurationJson);

            if (doc.RootElement.TryGetProperty("recordType", out var recordType))
            {
                return recordType
                    .GetString()?
                    .Trim()
                    .ToUpperInvariant()
                    ?? "A";
            }
        }
        catch (JsonException)
        {
        }

        return "A";
    }

    private static ObservationResult Failure(
        string code,
        string message) =>
        new(
            false,
            "Warning",
            "application/json",
            "{}",
            message,
            ErrorCode: code,
            ErrorMessage: message);
}
