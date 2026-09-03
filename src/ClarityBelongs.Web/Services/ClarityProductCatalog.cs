using ClarityBelongs.Web.Domain;

namespace ClarityBelongs.Web.Services;

public sealed record ClarityProduct(
    string Slug,
    string Name,
    string ShortDescription,
    string Outcome,
    string TargetLabel,
    string TargetPlaceholder,
    string TargetType,
    string MonitorType,
    string AdapterType,
    string SourceConfigurationJson,
    int DefaultCadenceMinutes,
    string DefaultImportance,
    string DefaultAlertRule,
    string HelpText);

public sealed class ClarityProductCatalog
{
    private static readonly IReadOnlyList<ClarityProduct> Products =
    [
        new(
            "website-change",
            "Website Change Monitor",
            "Know when the content of a public webpage changes.",
            "Clarity keeps a history of the page and surfaces meaningful before/after changes.",
            "Webpage URL",
            "https://example.com/pricing",
            "WebPage",
            "WebsiteChange",
            AdapterTypes.Http,
            "{\"mode\":\"content\"}",
            60,
            "Normal",
            "AnyMeaningfulChange",
            "Best for pricing pages, policies, product pages, public notices, and other pages you would otherwise revisit manually."),
        new(
            "website-uptime",
            "Website Uptime Monitor",
            "Know when a website stops responding or recovers.",
            "Clarity watches the HTTP result without treating normal response-time variation as a content change.",
            "Website URL",
            "https://example.com",
            "Website",
            "WebsiteUptime",
            AdapterTypes.Http,
            "{\"mode\":\"uptime\"}",
            5,
            "High",
            "AnyMeaningfulChange",
            "Use this for websites and public endpoints where availability matters."),
        new(
            "ssl-expiration",
            "SSL Expiration Monitor",
            "Track certificate identity and expiration.",
            "Clarity records the current certificate and makes certificate changes visible.",
            "HTTPS site or host",
            "https://example.com",
            "TlsEndpoint",
            "SslExpiration",
            AdapterTypes.Tls,
            "{}",
            720,
            "High",
            "AnyMeaningfulChange",
            "Clarity observes the public TLS certificate presented by the endpoint. Expiration state is included in every snapshot."),
        new(
            "domain-expiration",
            "Domain Expiration Monitor",
            "Track the expiration date published by the domain registry.",
            "Clarity uses RDAP data to keep domain registration expiration visible.",
            "Domain",
            "example.com",
            "Domain",
            "DomainExpiration",
            AdapterTypes.Domain,
            "{}",
            1440,
            "High",
            "AnyMeaningfulChange",
            "Registry data availability varies by TLD. Clarity stores the evidence returned by the RDAP lookup."),
        new(
            "dns-change",
            "DNS Change Monitor",
            "Know when a hostname starts resolving somewhere different.",
            "Clarity stores the normalized public address set and reports changes.",
            "Hostname",
            "example.com",
            "DnsRecordSet",
            "DnsChange",
            AdapterTypes.Dns,
            "{}",
            60,
            "High",
            "AnyMeaningfulChange",
            "V1 observes the public IP address set returned by DNS. More DNS record types can be added without changing the Clarity history model.")
    ];

    public IReadOnlyList<ClarityProduct> GetAll() => Products;

    public ClarityProduct? GetBySlug(string? slug) => Products
        .FirstOrDefault(x => string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
