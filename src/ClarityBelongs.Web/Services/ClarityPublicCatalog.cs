using ClarityBelongs.Web.Domain;

namespace ClarityBelongs.Web.Services;

public static class ClarityPublicCatalog
{
    private sealed record PublicProductSpec(
        string Slug,
        string Family,
        string ExpectedAdapterType,
        string ExpectedConfigurationJson);

    private static readonly PublicProductSpec[] Specs =
    [
        new("website-uptime", "Website availability", AdapterTypes.Http, "{\"mode\":\"uptime\"}"),
        new("http-status", "Website availability", AdapterTypes.Http, "{\"mode\":\"uptime\"}"),
        new("redirect-chain", "Website availability", AdapterTypes.Http, "{\"mode\":\"uptime\"}"),
        new("broken-link", "Website availability", AdapterTypes.Http, "{\"mode\":\"uptime\"}"),
        new("ssl-expiration", "Domain & certificate", AdapterTypes.Tls, "{}"),
        new("domain-expiration", "Domain & certificate", AdapterTypes.Domain, "{}"),
        new("dns-change", "DNS records", AdapterTypes.Dns, "{}"),
        new("nameserver-change", "DNS records", AdapterTypes.DnsRecord, "{\"recordType\":\"NS\"}"),
        new("mx-record", "DNS records", AdapterTypes.DnsRecord, "{\"recordType\":\"MX\"}"),
        new("spf-record", "DNS records", AdapterTypes.DnsRecord, "{\"recordType\":\"TXT\"}"),
        new("dkim-record", "DNS records", AdapterTypes.DnsRecord, "{\"recordType\":\"TXT\"}"),
        new("dmarc-record", "DNS records", AdapterTypes.DnsRecord, "{\"recordType\":\"TXT\"}"),
        new("api-endpoint-uptime", "Website availability", AdapterTypes.Http, "{\"mode\":\"uptime\"}"),
        new("service-outage", "Website availability", AdapterTypes.Http, "{\"mode\":\"uptime\"}"),
        new("website-change", "Website change", AdapterTypes.Http, "{\"mode\":\"content\"}")
    ];

    public static IReadOnlyList<string> Slugs => Specs
        .Select(x => x.Slug)
        .ToArray();

    public static IReadOnlyList<ClarityProduct> GetAll(
        ClarityProductCatalog catalog) => Specs
        .Select(x => Resolve(catalog, x))
        .ToArray();

    public static ClarityProduct? GetBySlug(
        ClarityProductCatalog catalog,
        string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var spec = Specs.FirstOrDefault(x => string.Equals(
            x.Slug,
            slug,
            StringComparison.OrdinalIgnoreCase));

        return spec is null
            ? null
            : Resolve(catalog, spec);
    }

    public static bool IsPublicSlug(
        ClarityProductCatalog catalog,
        string? slug) => GetBySlug(catalog, slug) is not null;

    private static ClarityProduct Resolve(
        ClarityProductCatalog catalog,
        PublicProductSpec spec)
    {
        var product = catalog.GetBySlug(spec.Slug)
            ?? throw new InvalidOperationException(
                $"Approved public product '{spec.Slug}' is missing from the internal catalog.");

        if (!string.Equals(
                product.AdapterType,
                spec.ExpectedAdapterType,
                StringComparison.Ordinal)
            || !string.Equals(
                product.SourceConfigurationJson,
                spec.ExpectedConfigurationJson,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Approved public product '{spec.Slug}' no longer matches its reviewed adapter contract.");
        }

        return ApplyPublicCopy(product with
        {
            Family = spec.Family
        });
    }

    private static ClarityProduct ApplyPublicCopy(ClarityProduct product) => product.Slug switch
    {
        "website-change" => product with
        {
            ShortDescription = "Detect when the fetched content of a public webpage changes.",
            Outcome = "Clarity stores a normalized page snapshot and compares subsequent observations by fingerprint. A changed fingerprint is recorded as a page change; Clarity does not interpret what the change means.",
            HelpText = "Best for stable public pages where any observed content change is useful. Dynamic pages may produce more change events."
        },
        "dns-change" => product with
        {
            HelpText = "Clarity observes the public IP address set returned by DNS and compares the normalized set over time."
        },
        "redirect-chain" => product with
        {
            HelpText = "Clarity records the final destination after following up to three redirects."
        },
        "broken-link" => product with
        {
            HelpText = "Clarity checks one specific public link per follow; it does not crawl an entire site."
        },
        "nameserver-change" => product with
        {
            HelpText = "Clarity resolves public NS records through its DNS record adapter and stores normalized answers as evidence."
        },
        "mx-record" => product with
        {
            HelpText = "Clarity resolves public MX records through its DNS record adapter and stores normalized answers as evidence."
        },
        "spf-record" => product with
        {
            HelpText = "Clarity records the public TXT answers returned for the domain. Use this when changes to published SPF-related TXT data matter."
        },
        "dkim-record" => product with
        {
            HelpText = "Clarity records the public TXT answers returned for the DKIM selector hostname you provide."
        },
        "dmarc-record" => product with
        {
            HelpText = "Clarity records the public TXT answers returned for the _dmarc hostname you provide."
        },
        "api-endpoint-uptime" => product with
        {
            HelpText = "Use a safe public health or status endpoint. Authenticated and private endpoints are not supported by this monitor."
        },
        "service-outage" => product with
        {
            HelpText = "Clarity checks public HTTP availability for the endpoint you provide; it does not infer outages from private account data."
        },
        _ => product
    };
}
