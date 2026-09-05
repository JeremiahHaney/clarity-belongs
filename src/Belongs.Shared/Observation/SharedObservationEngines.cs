using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Belongs.Shared.Observation;

public sealed record HttpProbeResult(
    bool Success,
    int StatusCode,
    long DurationMilliseconds,
    Uri FinalUri,
    string? Body,
    string? ContentType);

public sealed record TlsProbeResult(
    string Host,
    string Subject,
    string Issuer,
    string Thumbprint,
    DateTime ExpiresUtc);

public sealed record DomainProbeResult(
    string Domain,
    DateTime ExpiresUtc);

public sealed class PublicEndpointGuard
{
    public async Task ValidateAsync(
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        if (uri.Scheme != Uri.UriSchemeHttp
            && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "Only http and https targets are supported.");
        }

        await ResolvePublicAddressesAsync(
            uri.Host,
            cancellationToken);
    }

    public async Task ValidateHostAsync(
        string host,
        CancellationToken cancellationToken = default)
    {
        await ResolvePublicAddressesAsync(
            host,
            cancellationToken);
    }

    public async Task<IPAddress[]> ResolvePublicAddressesAsync(
        string host,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("A host name is required.");

        var normalizedHost = host.Trim().TrimEnd('.');

        if (IPAddress.TryParse(normalizedHost, out var literal))
        {
            EnsurePublic(literal);
            return [literal];
        }

        var addresses = await Dns.GetHostAddressesAsync(
            normalizedHost,
            cancellationToken);

        if (addresses.Length == 0)
            throw new InvalidOperationException("The target host did not resolve.");

        foreach (var address in addresses)
            EnsurePublic(address);

        return addresses;
    }

    private static void EnsurePublic(IPAddress address)
    {
        if (!IsPublic(address))
        {
            throw new InvalidOperationException(
                "Private, loopback, link-local, multicast, and other non-public addresses cannot be monitored.");
        }
    }

    internal static bool IsPublic(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)
            || ip.Equals(IPAddress.Any)
            || ip.Equals(IPAddress.IPv6Any)
            || ip.Equals(IPAddress.None))
        {
            return false;
        }

        if (ip.IsIPv4MappedToIPv6)
            return IsPublic(ip.MapToIPv4());

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();

            if (bytes[0] == 10
                || bytes[0] == 127
                || bytes[0] == 0)
            {
                return false;
            }

            if (bytes[0] == 169
                && bytes[1] == 254)
            {
                return false;
            }

            if (bytes[0] == 172
                && bytes[1] >= 16
                && bytes[1] <= 31)
            {
                return false;
            }

            if (bytes[0] == 192
                && bytes[1] == 168)
            {
                return false;
            }

            if (bytes[0] == 100
                && bytes[1] >= 64
                && bytes[1] <= 127)
            {
                return false;
            }

            if (bytes[0] >= 224)
                return false;

            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal
                || ip.IsIPv6Multicast
                || ip.IsIPv6SiteLocal
                || ip.Equals(IPAddress.IPv6Loopback))
            {
                return false;
            }

            var bytes = ip.GetAddressBytes();

            if ((bytes[0] & 0xFE) == 0xFC)
                return false;

            if (bytes.Take(15).All(x => x == 0)
                && bytes[15] <= 1)
            {
                return false;
            }

            return true;
        }

        return false;
    }
}

public static class PublicHttpMessageHandlerFactory
{
    public static HttpMessageHandler Create()
    {
        var guard = new PublicEndpointGuard();

        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var addresses = await guard.ResolvePublicAddressesAsync(
                    context.DnsEndPoint.Host,
                    cancellationToken);
                Exception? lastError = null;

                foreach (var address in addresses)
                {
                    var socket = new Socket(
                        address.AddressFamily,
                        SocketType.Stream,
                        ProtocolType.Tcp);

                    try
                    {
                        await socket.ConnectAsync(
                            new IPEndPoint(
                                address,
                                context.DnsEndPoint.Port),
                            cancellationToken);

                        return new NetworkStream(
                            socket,
                            ownsSocket: true);
                    }
                    catch (Exception ex) when (ex is SocketException or IOException)
                    {
                        lastError = ex;
                        socket.Dispose();
                    }
                }

                throw new HttpRequestException(
                    "The public target could not be reached.",
                    lastError);
            }
        };
    }
}

public sealed class HttpObservationEngine
{
    private static readonly HttpClient PublicClient = new(
        PublicHttpMessageHandlerFactory.Create(),
        disposeHandler: true);

    private readonly HttpClient _http;
    private readonly PublicEndpointGuard _guard;

    public HttpObservationEngine(
        HttpClient http,
        PublicEndpointGuard guard,
        bool usePinnedTransport = true)
    {
        _http = usePinnedTransport
            ? PublicClient
            : http;
        _guard = guard;
    }

    public async Task<HttpProbeResult> ObserveAsync(
        Uri target,
        bool captureBody,
        string userAgent,
        int maxRedirects = 3,
        CancellationToken cancellationToken = default)
    {
        var current = target;

        for (var redirect = 0; redirect <= maxRedirects; redirect++)
        {
            await _guard.ValidateAsync(
                current,
                cancellationToken);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                current);
            request.Headers.UserAgent.ParseAdd(userAgent);

            var watch = Stopwatch.StartNew();
            using var response = await _http.SendAsync(
                request,
                captureBody
                    ? HttpCompletionOption.ResponseContentRead
                    : HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            watch.Stop();

            if ((int)response.StatusCode is >= 300 and < 400
                && response.Headers.Location is not null)
            {
                if (redirect == maxRedirects)
                    throw new InvalidOperationException("Too many redirects.");

                current = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(
                        current,
                        response.Headers.Location);

                continue;
            }

            var body = captureBody
                ? await response.Content.ReadAsStringAsync(cancellationToken)
                : null;

            var statusCode = (int)response.StatusCode;

            return new HttpProbeResult(
                statusCode is >= 200 and < 400,
                statusCode,
                watch.ElapsedMilliseconds,
                current,
                body,
                response.Content.Headers.ContentType?.MediaType);
        }

        throw new InvalidOperationException("Unable to observe the target.");
    }
}

public interface ITlsCertificateProbe
{
    Task<X509Certificate2> GetCertificateAsync(
        Uri uri,
        IReadOnlyList<IPAddress> validatedAddresses,
        CancellationToken cancellationToken = default);
}

public sealed class SystemTlsCertificateProbe : ITlsCertificateProbe
{
    public async Task<X509Certificate2> GetCertificateAsync(
        Uri uri,
        IReadOnlyList<IPAddress> validatedAddresses,
        CancellationToken cancellationToken = default)
    {
        Exception? lastError = null;

        foreach (var address in validatedAddresses)
        {
            using var tcp = new TcpClient(address.AddressFamily);

            try
            {
                await tcp.ConnectAsync(
                    address,
                    uri.IsDefaultPort ? 443 : uri.Port,
                    cancellationToken);

                await using var ssl = new SslStream(
                    tcp.GetStream(),
                    false,
                    (_, _, _, errors) => errors == SslPolicyErrors.None);

                await ssl.AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions
                    {
                        TargetHost = uri.Host
                    },
                    cancellationToken);

                if (ssl.RemoteCertificate is null)
                {
                    throw new AuthenticationException(
                        "The server did not present an SSL certificate.");
                }

                return new X509Certificate2(ssl.RemoteCertificate);
            }
            catch (Exception ex) when (
                ex is SocketException
                    or IOException
                    or AuthenticationException)
            {
                lastError = ex;
            }
        }

        throw new AuthenticationException(
            "The TLS endpoint could not be validated.",
            lastError);
    }
}

public sealed class TlsObservationEngine
{
    private readonly PublicEndpointGuard _guard;
    private readonly ITlsCertificateProbe _probe;

    public TlsObservationEngine(
        PublicEndpointGuard guard,
        ITlsCertificateProbe? probe = null)
    {
        _guard = guard;
        _probe = probe ?? new SystemTlsCertificateProbe();
    }

    public async Task<TlsProbeResult> ObserveAsync(
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("TLS monitoring requires HTTPS.");

        var addresses = await _guard.ResolvePublicAddressesAsync(
            uri.Host,
            cancellationToken);

        using var cert = await _probe.GetCertificateAsync(
            uri,
            addresses,
            cancellationToken);

        return new TlsProbeResult(
            uri.Host,
            cert.Subject,
            cert.Issuer,
            cert.Thumbprint,
            cert.NotAfter.ToUniversalTime());
    }
}

public interface IDnsAddressResolver
{
    Task<IPAddress[]> ResolveAsync(
        string host,
        CancellationToken cancellationToken = default);
}

public sealed class SystemDnsAddressResolver : IDnsAddressResolver
{
    private readonly PublicEndpointGuard _guard = new();

    public Task<IPAddress[]> ResolveAsync(
        string host,
        CancellationToken cancellationToken = default) =>
        _guard.ResolvePublicAddressesAsync(
            host,
            cancellationToken);
}

public sealed class DnsObservationEngine
{
    private readonly IDnsAddressResolver _resolver;

    public DnsObservationEngine(IDnsAddressResolver? resolver = null)
    {
        _resolver = resolver ?? new SystemDnsAddressResolver();
    }

    public async Task<string[]> ObserveAddressesAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        var host = NormalizeHost(input);

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("A host name is required.");

        var addresses = await _resolver.ResolveAsync(
            host,
            cancellationToken);

        if (addresses.Length == 0)
            return [];

        foreach (var address in addresses)
        {
            if (!PublicEndpointGuard.IsPublic(address))
            {
                throw new InvalidOperationException(
                    "Private, loopback, link-local, multicast, and other non-public addresses cannot be monitored.");
            }
        }

        return addresses
            .Select(address => address.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(address => address, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string NormalizeHost(string input)
    {
        var value = (input ?? string.Empty).Trim();

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            value = uri.Host;

        return value
            .Trim()
            .TrimEnd('.');
    }
}

public sealed class DomainObservationEngine(HttpClient http)
{
    public async Task<DomainProbeResult> ObserveAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        var domain = DnsObservationEngine
            .NormalizeHost(input)
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(domain)
            || !domain.Contains('.'))
        {
            throw new InvalidOperationException("Enter a valid public domain name.");
        }

        using var response = await http.GetAsync(
            $"https://rdap.org/domain/{Uri.EscapeDataString(domain)}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"RDAP lookup returned HTTP {(int)response.StatusCode}.");
        }

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("events", out var events))
        {
            throw new InvalidOperationException(
                "The registry response did not include domain lifecycle events.");
        }

        foreach (var item in events.EnumerateArray())
        {
            if (!item.TryGetProperty("eventAction", out var action)
                || !string.Equals(
                    action.GetString(),
                    "expiration",
                    StringComparison.OrdinalIgnoreCase)
                || !item.TryGetProperty("eventDate", out var date)
                || !DateTimeOffset.TryParse(date.GetString(), out var parsed))
            {
                continue;
            }

            return new DomainProbeResult(
                domain,
                parsed.UtcDateTime);
        }

        throw new InvalidOperationException("Expiration date unavailable.");
    }
}

public static class SchedulingEngine
{
    public static DateTime NextRunUtc(
        DateTime nowUtc,
        int cadenceMinutes) =>
        nowUtc.AddMinutes(Math.Max(1, cadenceMinutes));

    public static DateTime RetryUtc(
        DateTime nowUtc,
        int failureCount,
        int maximumDelayMinutes = 60)
    {
        var exponent = Math.Clamp(failureCount, 0, 6);
        var delay = Math.Min(
            maximumDelayMinutes,
            (int)Math.Pow(2, exponent));

        return nowUtc.AddMinutes(Math.Max(1, delay));
    }
}

public static class NotificationEngine
{
    public static string BuildDedupKey(params object?[] parts) =>
        string.Join(
            ':',
            parts.Select(part =>
                Convert.ToString(part)?.Trim().ToLowerInvariant() ?? string.Empty));
}
