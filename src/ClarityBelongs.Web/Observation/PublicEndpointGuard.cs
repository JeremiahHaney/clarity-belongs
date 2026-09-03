using System.Net;
using System.Net.Sockets;

namespace ClarityBelongs.Web.Observation;

public sealed class PublicEndpointGuard
{
    public async Task ValidateAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Only http and https targets are supported.");

        if (string.IsNullOrWhiteSpace(uri.Host))
            throw new InvalidOperationException("A host name is required.");

        if (IPAddress.TryParse(uri.Host, out var literal))
        {
            if (!IsPublic(literal))
                throw new InvalidOperationException("Private, loopback, link-local, and unspecified addresses cannot be monitored.");

            return;
        }

        var addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        if (addresses.Length == 0)
            throw new InvalidOperationException("The target host did not resolve.");

        if (addresses.Any(address => !IsPublic(address)))
            throw new InvalidOperationException("The target resolves to a private or local address and cannot be monitored.");
    }

    private static bool IsPublic(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.None))
            return false;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();

            if (bytes[0] == 10 || bytes[0] == 127 || bytes[0] == 0)
                return false;

            if (bytes[0] == 169 && bytes[1] == 254)
                return false;

            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return false;

            if (bytes[0] == 192 && bytes[1] == 168)
                return false;

            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
                return false;

            if (bytes[0] >= 224)
                return false;

            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6Multicast || ip.IsIPv6SiteLocal || ip.Equals(IPAddress.IPv6Loopback))
                return false;

            var bytes = ip.GetAddressBytes();
            return (bytes[0] & 0xFE) != 0xFC;
        }

        return false;
    }
}
