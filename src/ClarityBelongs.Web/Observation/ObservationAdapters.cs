using Belongs.Shared.Observation;
using ClarityBelongs.Web.Domain;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
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
    public string Fingerprint => Convert.ToHexString(
        SHA256.HashData(
            Encoding.UTF8.GetBytes(NormalizedDataJson)));
}

public interface IObservationAdapter
{
    string AdapterType { get; }

    Task<ObservationResult> ObserveAsync(
        Target target,
        SourceDefinition source,
        CancellationToken cancellationToken = default);
}

public sealed class HttpObservationAdapter(
    HttpClient http,
    bool usePinnedTransport = true) : IObservationAdapter
{
    private readonly HttpObservationEngine _shared =
        new(
            http,
            new Belongs.Shared.Observation.PublicEndpointGuard(),
            usePinnedTransport);

    public string AdapterType => AdapterTypes.Http;

    public async Task<ObservationResult> ObserveAsync(
        Target target,
        SourceDefinition source,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(target.PrimaryUri, UriKind.Absolute, out var uri))
            return Failure("invalid_uri", "The target URL is invalid.");

        var mode = GetMode(source.ConfigurationJson);

        try
        {
            var result = await _shared.ObserveAsync(
                uri,
                mode == "content",
                "ClarityBelongs/0.6",
                cancellationToken: cancellationToken);

            var normalized = mode == "content"
                ? JsonSerializer.Serialize(new
                {
                    finalUrl = result.FinalUri.ToString(),
                    statusCode = result.StatusCode,
                    body = result.Body
                })
                : JsonSerializer.Serialize(new
                {
                    finalUrl = result.FinalUri.ToString(),
                    statusCode = result.StatusCode
                });

            return new ObservationResult(
                true,
                result.Success ? "Healthy" : "Down",
                result.ContentType ?? "text/plain",
                normalized,
                $"HTTP {result.StatusCode} in {result.DurationMilliseconds} ms",
                result.StatusCode);
        }
        catch (Exception ex) when (
            ex is HttpRequestException
                or SocketException
                or IOException
                or InvalidOperationException
                or TaskCanceledException)
        {
            return Failure("http_exception", ex.Message);
        }
    }

    private static string GetMode(string? configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
            return "content";

        try
        {
            using var doc = JsonDocument.Parse(configurationJson);

            if (doc.RootElement.TryGetProperty("mode", out var mode))
                return mode.GetString()?.ToLowerInvariant() ?? "content";
        }
        catch (JsonException)
        {
        }

        return "content";
    }

    private static ObservationResult Failure(
        string code,
        string message) =>
        new(
            false,
            "Down",
            "application/json",
            "{}",
            message,
            ErrorCode: code,
            ErrorMessage: message);
}

public sealed class TlsObservationAdapter : IObservationAdapter
{
    private readonly TlsObservationEngine _shared;

    public TlsObservationAdapter()
        : this(
            new TlsObservationEngine(
                new Belongs.Shared.Observation.PublicEndpointGuard()))
    {
    }

    internal TlsObservationAdapter(TlsObservationEngine shared)
    {
        _shared = shared;
    }

    public string AdapterType => AdapterTypes.Tls;

    public async Task<ObservationResult> ObserveAsync(
        Target target,
        SourceDefinition source,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreateHttpsUri(target.PrimaryUri, out var uri))
        {
            return Failure(
                "invalid_uri",
                "Enter a valid HTTPS site or host.");
        }

        try
        {
            var result = await _shared.ObserveAsync(
                uri,
                cancellationToken);

            var days = (int)Math.Ceiling(
                (result.ExpiresUtc - DateTime.UtcNow).TotalDays);

            var normalized = JsonSerializer.Serialize(new
            {
                host = result.Host,
                subject = result.Subject,
                issuer = result.Issuer,
                thumbprint = result.Thumbprint,
                expiresUtc = result.ExpiresUtc
            });

            return new ObservationResult(
                days >= 0,
                days < 0
                    ? "Down"
                    : days <= 30
                        ? "Warning"
                        : "Healthy",
                "application/json",
                normalized,
                $"TLS expires {result.ExpiresUtc:yyyy-MM-dd} UTC ({Math.Max(0, days)} days)",
                ErrorCode: days >= 0 ? null : "tls_expired",
                ErrorMessage: days >= 0 ? null : "The TLS certificate is expired.");
        }
        catch (Exception ex) when (
            ex is SocketException
                or AuthenticationException
                or IOException
                or InvalidOperationException)
        {
            return Failure(
                "tls_error",
                ex.Message);
        }
    }

    private static bool TryCreateHttpsUri(
        string input,
        out Uri uri)
    {
        uri = null!;
        var value = input.Trim();

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
        {
            if (absolute.Scheme != Uri.UriSchemeHttps)
                return false;

            uri = absolute;
            return true;
        }

        if (!Uri.TryCreate(
                $"https://{value}",
                UriKind.Absolute,
                out var created)
            || created is null
            || created.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(created.Host))
        {
            return false;
        }

        uri = created;
        return true;
    }

    private static ObservationResult Failure(
        string code,
        string message) =>
        new(
            false,
            "Down",
            "application/json",
            "{}",
            message,
            ErrorCode: code,
            ErrorMessage: message);
}

public sealed class DnsObservationAdapter : IObservationAdapter
{
    private readonly DnsObservationEngine _shared;

    public DnsObservationAdapter()
        : this(new DnsObservationEngine())
    {
    }

    internal DnsObservationAdapter(DnsObservationEngine shared)
    {
        _shared = shared;
    }

    public string AdapterType => AdapterTypes.Dns;

    public async Task<ObservationResult> ObserveAsync(
        Target target,
        SourceDefinition source,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var host = NormalizeHost(target.PrimaryUri);
            var values = await _shared.ObserveAddressesAsync(
                host,
                cancellationToken);

            var json = JsonSerializer.Serialize(new
            {
                host,
                addresses = values
            });

            return new ObservationResult(
                values.Length > 0,
                values.Length > 0 ? "Healthy" : "Down",
                "application/json",
                json,
                values.Length > 0
                    ? $"Resolved: {string.Join(", ", values)}"
                    : "The hostname did not resolve.",
                ErrorCode: values.Length > 0 ? null : "dns_empty",
                ErrorMessage: values.Length > 0 ? null : "The hostname did not resolve.");
        }
        catch (Exception ex) when (
            ex is SocketException
                or ArgumentException
                or InvalidOperationException)
        {
            return new ObservationResult(
                false,
                "Down",
                "application/json",
                "{}",
                ex.Message,
                ErrorCode: "dns_error",
                ErrorMessage: ex.Message);
        }
    }

    public static string NormalizeHost(string input) =>
        DnsObservationEngine.NormalizeHost(input);
}

public sealed class DomainObservationAdapter(
    HttpClient http) : IObservationAdapter
{
    private readonly DomainObservationEngine _shared = new(http);

    public string AdapterType => AdapterTypes.Domain;

    public async Task<ObservationResult> ObserveAsync(
        Target target,
        SourceDefinition source,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shared.ObserveAsync(
                target.PrimaryUri,
                cancellationToken);

            var days = (int)Math.Ceiling(
                (result.ExpiresUtc - DateTime.UtcNow).TotalDays);

            var json = JsonSerializer.Serialize(new
            {
                domain = result.Domain,
                expiresUtc = result.ExpiresUtc
            });

            return new ObservationResult(
                days >= 0,
                days < 0
                    ? "Down"
                    : days <= 30
                        ? "Warning"
                        : "Healthy",
                "application/json",
                json,
                $"Domain expires {result.ExpiresUtc:yyyy-MM-dd} UTC ({Math.Max(0, days)} days)",
                ErrorCode: days >= 0 ? null : "domain_expired",
                ErrorMessage: days >= 0 ? null : "The domain registration is expired.");
        }
        catch (Exception ex) when (
            ex is HttpRequestException
                or JsonException
                or InvalidOperationException
                or TaskCanceledException)
        {
            return Failure("rdap_error", ex.Message);
        }
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
