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
        return product.Slug.ToLowerInvariant() switch
        {
            "website-change" => product with
            {
                Outcome = "Clarity stores the page response over time and shows before-and-after evidence when that whole-page content changes.",
                HelpText = "Best for relatively stable public pages where any whole-page content change is worth reviewing."
            },
            "ssl-expiration" => product with
            {
                HelpText = "Clarity keeps the observed public certificate and expiration state available in My Clarity."
            },
            "dns-change" => product with
            {
                HelpText = "Clarity observes the public IP address set returned by DNS."
            },
            "redirect-chain" => product with
            {
                HelpText = "Clarity records the final public destination after following up to three redirects."
            },
            "broken-link" => product with
            {
                HelpText = "Clarity checks the specific public link you provide rather than crawling an entire site."
            },
            "api-endpoint-uptime" => product with
            {
                HelpText = "Use a safe public health or status endpoint. Authenticated and private endpoints are not supported."
            },
            "service-outage" => product with
            {
                HelpText = "Clarity observes public HTTP availability for the endpoint you provide; it does not infer outages from private account data."
            },
            _ => product
        };
    }
}
