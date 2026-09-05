namespace ClarityBelongs.Web.Services;

public static class PublicSiteCatalog
{
    private static readonly HashSet<string> Phase1ProductSlugs = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "website-uptime",
        "http-status",
        "redirect-chain",
        "broken-link",
        "ssl-expiration",
        "domain-expiration",
        "dns-change",
        "nameserver-change",
        "mx-record",
        "spf-record",
        "dkim-record",
        "dmarc-record",
        "api-endpoint-uptime",
        "service-outage"
    };

    public static readonly IReadOnlyList<string> StaticPublicPaths =
    [
        "/",
        "/products",
        "/learn",
        "/pricing",
        "/about",
        "/support",
        "/contact",
        "/privacy",
        "/terms"
    ];

    public static IReadOnlyList<ClarityProduct> GetProducts(
        ClarityProductCatalog catalog) =>
        catalog
            .GetAll()
            .Where(product => Phase1ProductSlugs.Contains(product.Slug))
            .ToArray();

    public static ClarityProduct? GetProduct(
        ClarityProductCatalog catalog,
        string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : GetProducts(catalog)
                .FirstOrDefault(product => string.Equals(
                    product.Slug,
                    slug,
                    StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<LearnEntry> GetLearnEntries(
        LearnContentCatalog catalog) =>
        catalog
            .GetAll()
            .Where(entry => Phase1ProductSlugs.Contains(entry.ProductSlug))
            .ToArray();

    public static LearnEntry? GetLearnEntry(
        LearnContentCatalog catalog,
        string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : GetLearnEntries(catalog)
                .FirstOrDefault(entry => string.Equals(
                    entry.Slug,
                    slug,
                    StringComparison.OrdinalIgnoreCase));

    public static bool IsPhase1ProductSlug(string slug) =>
        Phase1ProductSlugs.Contains(slug);
}
