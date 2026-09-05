namespace ClarityBelongs.Web.Services;

public sealed record PublicSeoMetadata(
    bool Indexable,
    string CanonicalUrl,
    string Title,
    string Description);

public static class PublicSiteSeoPolicy
{
    private static readonly Dictionary<string, (string Title, string Description)> StaticMetadata =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["/"] = ("Clarity Belongs - Keep an eye on what matters", "Clarity Belongs monitors public websites, endpoints, domains, DNS, certificates, and service status so changes and outages stay visible."),
            ["/products"] = ("What Clarity Can Watch - Clarity Belongs", "Explore the approved Phase 1 monitoring capabilities available in Clarity Belongs."),
            ["/learn"] = ("Learn - Clarity Belongs", "Plain-language guides for public monitoring problems Clarity Belongs can solve today."),
            ["/pricing"] = ("Pricing - Clarity Belongs", "Compare Clarity Belongs monitoring plans, limits, cadence, history, and notification options."),
            ["/about"] = ("About Clarity Belongs", "Clarity Belongs helps people see what changed, understand what matters, and stop checking the same things manually."),
            ["/support"] = ("Support - Clarity Belongs", "Clarity Belongs support and help for accounts, follows, monitoring history, and alerts."),
            ["/contact"] = ("Contact - Clarity Belongs", "Contact Clarity Belongs for product feedback and support questions."),
            ["/privacy"] = ("Privacy - Clarity Belongs", "Clarity Belongs privacy principles and the data used to provide accounts, monitoring history, alerts, and billing."),
            ["/terms"] = ("Terms - Clarity Belongs", "Terms for using Clarity Belongs monitoring, account, notification, and billing services.")
        };

    public static PublicSeoMetadata Resolve(
        string path,
        ClarityProductCatalog products)
    {
        path = NormalizePath(path);

        if (StaticMetadata.TryGetValue(path, out var item))
            return Public(path, item.Title, item.Description);

        if (path.StartsWith("/products/", StringComparison.OrdinalIgnoreCase))
        {
            var slug = path[10..];
            var product = PublicSiteCatalog.GetProduct(products, slug);
            return product is null
                ? Private()
                : Public(
                    $"/products/{product.Slug}",
                    $"{product.Name} - Clarity Belongs",
                    product.ShortDescription);
        }

        if (path.StartsWith("/learn/", StringComparison.OrdinalIgnoreCase))
        {
            var slug = path[7..];
            var catalog = new LearnContentCatalog();
            var entry = PublicSiteCatalog.GetLearnEntry(catalog, slug);
            return entry is null
                ? Private()
                : Public(
                    $"/learn/{entry.Slug}",
                    $"{entry.Title} - Clarity Belongs",
                    entry.Description);
        }

        return Private();
    }

    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        var clean = path
            .Split('?', '#')[0]
            .Trim();

        if (!clean.StartsWith('/'))
            clean = "/" + clean;

        if (clean.Length > 1)
            clean = clean.TrimEnd('/');

        return clean;
    }

    private static PublicSeoMetadata Public(
        string path,
        string title,
        string description) =>
        new(
            true,
            $"https://claritybelongs.com{(path == "/" ? "/" : path.ToLowerInvariant())}",
            title,
            description);

    private static PublicSeoMetadata Private() =>
        new(false, string.Empty, string.Empty, string.Empty);
}
