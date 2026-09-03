using ClarityBelongs.Web.Domain;
using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace ClarityBelongs.Web.Observation;

public sealed record ObservationResult(
    bool Success,
    string Status,
    string ContentType,
    string NormalizedDataJson,
    string Summary,
    int? HttpStatusCode = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public string Fingerprint => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(NormalizedDataJson)));
}

public interface IObservationAdapter
{
    string AdapterType { get; }
    Task<ObservationResult> ObserveAsync(Target target, SourceDefinition source, CancellationToken cancellationToken = default);
}

public sealed class HttpObservationAdapter(HttpClient http, PublicEndpointGuard guard) : IObservationAdapter
{
    private const int MaxRedirects = 3;
    public string AdapterType => AdapterTypes.Http;

    public async Task<ObservationResult> ObserveAsync(Target target, SourceDefinition source, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(target.PrimaryUri, UriKind.Absolute, out var current))
            return Failure("invalid_uri", "The target URL is invalid.");

        try
        {
            for (var redirect = 0; redirect <= MaxRedirects; redirect++)
            {
                await guard.ValidateAsync(current, cancellationToken);
                using var request = new HttpRequestMessage(HttpMethod.Get, current);
                request.Headers.UserAgent.ParseAdd("ClarityBelongs/0.1");

                var watch = Stopwatch.StartNew();
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
                watch.Stop();

                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is not null)
                {
                    if (redirect == MaxRedirects)
                        return Failure("redirect_limit", "Too many redirects.");

                    current = response.Headers.Location.IsAbsoluteUri
                        ? response.Headers.Location
                        : new Uri(current, response.Headers.Location);

                    continue;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var normalized = JsonSerializer.Serialize(new
                {
                    finalUrl = current.ToString(),
                    statusCode = (int)response.StatusCode,
                    responseMilliseconds = watch.ElapsedMilliseconds,
                    body
                });

                return new ObservationResult(
                    response.IsSuccessStatusCode,
                    response.IsSuccessStatusCode ? "Healthy" : "Down",
                    response.Content.Headers.ContentType?.MediaType ?? "text/plain",
                    normalized,
                    $"HTTP {(int)response.StatusCode} in {watch.ElapsedMilliseconds} ms",
                    (int)response.StatusCode,
                    response.IsSuccessStatusCode ? null : "http_error",
                    response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or SocketException or IOException or InvalidOperationException or TaskCanceledException)
        {
            return Failure("http_exception", ex.Message);
        }

        return Failure("unknown", "Unable to observe the target.");
    }

    private static ObservationResult Failure(string code, string message) =>
        new(false, "Down", "application/json", "{}", message, ErrorCode: code, ErrorMessage: message);
}

public sealed class TlsObservationAdapter(PublicEndpointGuard guard) : IObservationAdapter
{
    public string AdapterType => AdapterTypes.Tls;

    public async Task<ObservationResult> ObserveAsync(Target target, SourceDefinition source, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(target.PrimaryUri, UriKind.Absolute, out var uri))
            uri = new Uri($"https://{target.PrimaryUri.Trim()}");

        try
        {
            await guard.ValidateAsync(uri, cancellationToken);
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(uri.Host, uri.IsDefaultPort ? 443 : uri.Port, cancellationToken);
            await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, errors) => errors == SslPolicyErrors.None);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = uri.Host }, cancellationToken);

            if (ssl.RemoteCertificate is null)
                throw new AuthenticationException("The server did not present an SSL certificate.");

            using var cert = new X509Certificate2(ssl.RemoteCertificate);
            var expiresUtc = cert.NotAfter.ToUniversalTime();
            var days = (int)Math.Ceiling((expiresUtc - DateTime.UtcNow).TotalDays);
            var normalized = JsonSerializer.Serialize(new
            {
                host = uri.Host,
                subject = cert.Subject,
                issuer = cert.Issuer,
                thumbprint = cert.Thumbprint,
                expiresUtc,
                daysRemaining = days
            });

            return new ObservationResult(
                days >= 0,
                days < 0 ? "Down" : days <= 30 ? "Warning" : "Healthy",
                "application/json",
                normalized,
                $"TLS expires {expiresUtc:yyyy-MM-dd} UTC ({Math.Max(0, days)} days)");
        }
        catch (Exception ex) when (ex is SocketException or AuthenticationException or IOException or InvalidOperationException)
        {
            return new ObservationResult(false, "Down", "application/json", "{}", ex.Message, ErrorCode: "tls_error", ErrorMessage: ex.Message);
        }
    }
}

public sealed class DnsObservationAdapter : IObservationAdapter
{
    public string AdapterType => AdapterTypes.Dns;

    public async Task<ObservationResult> ObserveAsync(Target target, SourceDefinition source, CancellationToken cancellationToken = default)
    {
        try
        {
            var host = NormalizeHost(target.PrimaryUri);
            var addresses = await System.Net.Dns.GetHostAddressesAsync(host, cancellationToken);
            var values = addresses.Select(x => x.ToString()).Distinct().OrderBy(x => x).ToArray();
            var json = JsonSerializer.Serialize(new { host, addresses = values });

            return new ObservationResult(
                values.Length > 0,
                values.Length > 0 ? "Healthy" : "Down",
                "application/json",
                json,
                values.Length > 0 ? $"Resolved: {string.Join(", ", values)}" : "The hostname did not resolve.");
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return new ObservationResult(false, "Down", "application/json", "{}", ex.Message, ErrorCode: "dns_error", ErrorMessage: ex.Message);
        }
    }

    public static string NormalizeHost(string input)
    {
        var value = (input ?? string.Empty).Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            value = uri.Host;

        return value.Trim().TrimEnd('.');
    }
}

public sealed class DomainObservationAdapter(HttpClient http) : IObservationAdapter
{
    public string AdapterType => AdapterTypes.Domain;

    public async Task<ObservationResult> ObserveAsync(Target target, SourceDefinition source, CancellationToken cancellationToken = default)
    {
        var domain = DnsObservationAdapter.NormalizeHost(target.PrimaryUri).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(domain) || !domain.Contains('.'))
            return Failure("invalid_domain", "Enter a valid public domain name.");

        try
        {
            using var response = await http.GetAsync($"https://rdap.org/domain/{Uri.EscapeDataString(domain)}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Failure("rdap_http", $"RDAP lookup returned HTTP {(int)response.StatusCode}.");

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("events", out var events))
                return Failure("rdap_events", "The registry response did not include domain lifecycle events.");

            DateTime? expiresUtc = null;
            foreach (var item in events.EnumerateArray())
            {
                if (!item.TryGetProperty("eventAction", out var action))
                    continue;

                if (!string.Equals(action.GetString(), "expiration", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!item.TryGetProperty("eventDate", out var date))
                    continue;

                if (DateTimeOffset.TryParse(date.GetString(), out var parsed))
                {
                    expiresUtc = parsed.UtcDateTime;
                    break;
                }
            }

            if (!expiresUtc.HasValue)
                return Failure("expiration_unavailable", "Expiration date unavailable.");

            var days = (int)Math.Ceiling((expiresUtc.Value - DateTime.UtcNow).TotalDays);
            var json = JsonSerializer.Serialize(new { domain, expiresUtc, daysRemaining = days });
            return new ObservationResult(
                days >= 0,
                days < 0 ? "Down" : days <= 30 ? "Warning" : "Healthy",
                "application/json",
                json,
                $"Domain expires {expiresUtc.Value:yyyy-MM-dd} UTC ({Math.Max(0, days)} days)");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return Failure("rdap_error", ex.Message);
        }
    }

    private static ObservationResult Failure(string code, string message) =>
        new(false, "Warning", "application/json", "{}", message, ErrorCode: code, ErrorMessage: message);
}
