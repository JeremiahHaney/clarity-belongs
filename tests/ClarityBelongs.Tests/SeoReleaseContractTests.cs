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
        "service-outage"
    ];

    private static readonly string[] ExpectedLearnSlugs =
    [
        "website-uptime-monitoring-for-small-sites",
        "ssl-certificate-expiration-alert",
        "domain-expiration-reminder",
        "dns-change-monitor"
    ];

    [Fact]
    public void PublicProductCatalogIsExactlyPhase1()
    {
        var catalog = new ClarityProductCatalog();
        var publicProducts = PublicSiteCatalog.GetProducts(catalog);

        Assert.Equal(
            ExpectedProductSlugs.OrderBy(value => value),
            publicProducts.Select(product => product.Slug).OrderBy(value => value));
        Assert.Equal(ExpectedProductSlugs.Length, publicProducts.Count);
        Assert.Equal(
            publicProducts.Count,
            publicProducts.Select(product => product.ShortDescription).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void PublicLearnCatalogOnlyTargetsPhase1Products()
    {
        var catalog = new LearnContentCatalog();
        var entries = PublicSiteCatalog.GetLearnEntries(catalog);

        Assert.Equal(
            ExpectedLearnSlugs.OrderBy(value => value),
            entries.Select(entry => entry.Slug).OrderBy(value => value));
        Assert.All(entries, entry => Assert.True(PublicSiteCatalog.IsPhase1ProductSlug(entry.ProductSlug)));
        Assert.Equal(
            entries.Count,
            entries.Select(entry => entry.Description).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void SitemapIsExactAndContainsNoDuplicateCanonicalUrls()
    {
        var document = XDocument.Load(RepoPath("src/ClarityBelongs.Web/wwwroot/sitemap.xml"));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = document
            .Descendants(ns + "loc")
            .Select(element => element.Value)
            .ToArray();

        var expected = PublicSiteCatalog.StaticPublicPaths
            .Select(ToCanonical)
            .Concat(ExpectedProductSlugs.Select(slug => ToCanonical($"/products/{slug}")))
            .Concat(ExpectedLearnSlugs.Select(slug => ToCanonical($"/learn/{slug}")))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(expected.Count, urls.Length);
        Assert.Equal(urls.Length, urls.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(expected, urls.ToHashSet(StringComparer.Ordinal));
        Assert.DoesNotContain(urls, url => url.Contains("/account", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(urls, url => url.Contains("/api/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(urls, url => url.Contains("feedback-ops", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/products")]
    [InlineData("/products/website-uptime")]
    [InlineData("/products/http-status")]
    [InlineData("/learn")]
    [InlineData("/learn/dns-change-monitor")]
    [InlineData("/pricing")]
    [InlineData("/about")]
    [InlineData("/support")]
    [InlineData("/contact")]
    [InlineData("/privacy")]
    [InlineData("/terms")]
    public void PublicRoutesHaveCanonicalMetadata(string path)
    {
        var metadata = PublicSiteSeoPolicy.Resolve(path, new ClarityProductCatalog());

        Assert.True(metadata.Indexable);
        Assert.Equal(ToCanonical(PublicSiteSeoPolicy.NormalizePath(path)), metadata.CanonicalUrl);
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
    [InlineData("/owner")]
    [InlineData("/admin")]
    [InlineData("/tools")]
    [InlineData("/does-not-exist")]
    [InlineData("/products/product-price")]
    [InlineData("/products/not-a-product")]
    [InlineData("/learn/how-to-monitor-a-website-for-changes")]
    [InlineData("/learn/not-a-guide")]
    public void PrivateHiddenAndInvalidRoutesAreNoindex(string path)
    {
        var metadata = PublicSiteSeoPolicy.Resolve(path, new ClarityProductCatalog());

        Assert.False(metadata.Indexable);
        Assert.Equal(string.Empty, metadata.CanonicalUrl);
    }

    [Fact]
    public void HiddenProductsAreExcludedFromPublicSeoPolicy()
    {
        var catalog = new ClarityProductCatalog();
        var publicSlugs = PublicSiteCatalog.GetProducts(catalog)
            .Select(product => product.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hidden = catalog.GetAll()
            .Where(product => !publicSlugs.Contains(product.Slug))
            .ToArray();

        Assert.NotEmpty(hidden);
        Assert.All(
            hidden,
            product => Assert.False(
                PublicSiteSeoPolicy.Resolve($"/products/{product.Slug}", catalog).Indexable));
    }

    [Fact]
    public void RobotsUsesOnlyCurrentSitemapAndBlocksPrivateRouteFamilies()
    {
        var robots = File.ReadAllText(RepoPath("src/ClarityBelongs.Web/wwwroot/robots.txt"));

        Assert.Contains("Sitemap: https://claritybelongs.com/sitemap.xml", robots, StringComparison.Ordinal);
        Assert.Equal(1, Count(robots, "Sitemap:"));
        Assert.Contains("Disallow: /api/", robots, StringComparison.Ordinal);
        Assert.Contains("Disallow: /feedback-ops", robots, StringComparison.Ordinal);
        Assert.Contains("Disallow: /follows/", robots, StringComparison.Ordinal);
        Assert.DoesNotContain("sitemap-index", robots, StringComparison.OrdinalIgnoreCase);
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
        Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "../../../../../",
                relativePath));
}
