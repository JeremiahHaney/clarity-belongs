namespace ClarityBelongs.Web.Services;

public sealed class PublicClarityProductCatalog(ClarityProductCatalog catalog)
{
    private static readonly HashSet<string> PublicSlugs = new(
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
        "service-outage",
        "website-change"
    };

    public IReadOnlyList<ClarityProduct> GetAll() => catalog
        .GetAll()
        .Where(product => PublicSlugs.Contains(product.Slug))
        .Select(CleanPublicCopy)
        .ToList();

    public ClarityProduct? GetBySlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)
            || !PublicSlugs.Contains(slug))
        {
            return null;
        }

        var product = catalog.GetBySlug(slug);
        return product is null
            ? null
            : CleanPublicCopy(product);
    }

    public bool IsPublic(string? slug) => !string.IsNullOrWhiteSpace(slug)
        && PublicSlugs.Contains(slug);

    private static ClarityProduct CleanPublicCopy(ClarityProduct product)
    {
        if (!product.Slug.Equals("website-change", StringComparison.OrdinalIgnoreCase))
            return product;

        return product with
        {
            Outcome = "Clarity stores the page response over time and shows before-and-after evidence when that whole-page content changes.",
            HelpText = "Best for relatively stable public pages where any whole-page content change is worth reviewing."
        };
    }
}
