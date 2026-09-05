using System.Xml.Linq;
using ClarityBelongs.Web.Services;

namespace ClarityBelongs.Tests;

public sealed class SeoReleaseContractTests
{
    private static readonly string[] ExpectedProductSlugs =
    [
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
    ];

    [Fact]
    public void PublicCatalogIsExactlyApprovedPhase1Surface()
    {
        var catalog = new PublicClarityProductCatalog(new ClarityProductCatalog());
        var products = catalog.GetAll();

        Assert.Equal(
            ExpectedProductSlugs.OrderBy(value => value),
            products.Select(product => product.Slug).OrderBy(value => value));
        Assert.Equal(
            products.Count,
            products.Select(product => product.ShortDescription).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void LearnEntriesOnlyPointToPublicProductsAndHaveUniqueDescriptions()
    {
        var products = new PublicClarityProductCatalog(new ClarityProductCatalog());
        var entries = new LearnContentCatalog().GetAll();

        Assert.NotEmpty(entries);
        Assert.All(entries, entry => Assert.True(products.IsPublic(entry.ProductSlug)));
        Assert.Equal(
            entries.Count,
            entries.Select(entry => entry.Description).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void SitemapExactlyMatchesApprovedPublicSurface()
    {
        var products = new PublicClarityProductCatalog(new ClarityProductCatalog());
        var learn = new LearnContentCatalog();
        var expected = PublicSiteSeoPolicy.StaticPublicPaths
            .Select(ToCanonical)
            .Concat(products.GetAll().Select(product => ToCanonical($"/products/{product.Slug}")))
            .Concat(
                learn.GetAll()
                    .Where(entry => products.IsPublic(entry.ProductSlug))
                    .Select(entry => ToCanonical($"/learn/{entry.Slug}")))
            .ToHashSet(StringComparer.Ordinal);
        var urls = SitemapUrls();

        Assert.Equal(expected.Count, urls.Length);
        Assert.Equal(urls.Length, urls.Distinct(StringComparer.Ordinal).Count());
        Assert.True(expected.SetEquals(urls));
        Assert.DoesNotContain("https://claritybelongs.com/tools", urls);
        Assert.DoesNotContain("https://claritybelongs.com/products/product-price", urls);
    }

    [Fact]
    public void HiddenProductsCannotResolveAsIndexablePages()
    {
        var internalCatalog = new ClarityProductCatalog();
        var products = new PublicClarityProductCatalog(internalCatalog);
        var publicSlugs = products
            .GetAll()
            .Select(product => product.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hidden = internalCatalog
            .GetAll()
            .Where(product => !publicSlugs.Contains(product.Slug))
            .ToArray();

        Assert.NotEmpty(hidden);
        Assert.All(
            hidden,
            product => Assert.False(
                Resolve($"/products/{product.Slug}").Indexable));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/products")]
    [InlineData("/products/website-uptime")]
    [InlineData("/products/HTTP-STATUS/")]
    [InlineData("/learn")]
    [InlineData("/learn/dns-change-monitor?source=test")]
    [InlineData("/pricing")]
    [InlineData("/about")]
    [InlineData("/support")]
    [InlineData("/contact")]
    [InlineData("/privacy")]
    [InlineData("/terms")]
    public void PublicRoutesHaveCanonicalMetadata(string path)
    {
        var metadata = Resolve(path);

        Assert.True(metadata.Indexable);
        Assert.StartsWith("https://claritybelongs.com/", metadata.CanonicalUrl, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(metadata.Title));
        Assert.False(string.IsNullOrWhiteSpace(metadata.Description));
    }

    [Theory]
    [InlineData("/login")]
    [InlineData("/signup")]
    [InlineData("/forgot-password")]
    [InlineData("/reset-password")]
    [InlineData("/account")]
    [InlineData("/settings")]
    [InlineData("/my-clarity")]
    [InlineData("/add")]
    [InlineData("/follows/123")]
    [InlineData("/follows/123/changes/456")]
    [InlineData("/feedback")]
    [InlineData("/feedback-ops")]
    [InlineData("/reliability-ops")]
    [InlineData("/operational-health")]
    [InlineData("/owner")]
    [InlineData("/admin")]
    [InlineData("/tools")]
    [InlineData("/api/follows")]
    [InlineData("/does-not-exist")]
    [InlineData("/products/product-price")]
    [InlineData("/products/not-a-product")]
    [InlineData("/learn/not-a-guide")]
    public void PrivateHiddenAndInvalidRoutesAreNoindex(string path)
    {
        var metadata = Resolve(path);

        Assert.False(metadata.Indexable);
        Assert.Equal(string.Empty, metadata.CanonicalUrl);
    }

    [Fact]
    public void ProductAndLearnCanonicalMetadataIsUnique()
    {
        var products = new PublicClarityProductCatalog(new ClarityProductCatalog());
        var learn = new LearnContentCatalog();
        var metadata = products
            .GetAll()
            .Select(product => Resolve($"/products/{product.Slug}"))
            .Concat(
                learn.GetAll()
                    .Where(entry => products.IsPublic(entry.ProductSlug))
                    .Select(entry => Resolve($"/learn/{entry.Slug}")))
            .ToArray();

        Assert.Equal(
            metadata.Length,
            metadata.Select(item => item.CanonicalUrl).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(
            products.GetAll().Count,
            products.GetAll().Select(product => product.ShortDescription).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void RobotsHasOneCurrentSitemapAndPrivateRouteRules()
    {
        var robots = File.ReadAllText(RepoPath("src/ClarityBelongs.Web/wwwroot/robots.txt"));

        Assert.Equal(1, Count(robots, "Sitemap:"));
        Assert.Contains("Sitemap: https://claritybelongs.com/sitemap.xml", robots, StringComparison.Ordinal);
        Assert.Contains("Disallow: /follows/", robots, StringComparison.Ordinal);
        Assert.Contains("Disallow: /feedback-ops", robots, StringComparison.Ordinal);
        Assert.Contains("Disallow: /reliability-ops", robots, StringComparison.Ordinal);
        Assert.Contains("Disallow: /operational-health", robots, StringComparison.Ordinal);
        Assert.Contains("Disallow: /api/", robots, StringComparison.Ordinal);
        Assert.Contains("Disallow: /auth/", robots, StringComparison.Ordinal);
        Assert.Contains("Disallow: /billing/", robots, StringComparison.Ordinal);
        Assert.DoesNotContain("sitemap-index", robots, StringComparison.OrdinalIgnoreCase);
    }

    private static PublicSeoMetadata Resolve(string path) =>
        PublicSiteSeoPolicy.Resolve(
            path,
            new PublicClarityProductCatalog(new ClarityProductCatalog()),
            new LearnContentCatalog());

    private static string[] SitemapUrls()
    {
        var document = XDocument.Load(RepoPath("src/ClarityBelongs.Web/wwwroot/sitemap.xml"));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        return document
            .Descendants(ns + "loc")
            .Select(element => element.Value)
            .ToArray();
    }

    private static string ToCanonical(string path) =>
        path == "/"
            ? "https://claritybelongs.com/"
            : $"https://claritybelongs.com{path.ToLowerInvariant()}";

    private static int Count(string value, string token)
    {
        var count = 0;
        var start = 0;

        while ((start = value.IndexOf(token, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += token.Length;
        }

        return count;
    }

    private static string RepoPath(string relativePath) =>
        Path.Combine(FindRepositoryRoot(), relativePath);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ClarityBelongs.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate ClarityBelongs.slnx.");
    }
}
