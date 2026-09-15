using ClarityBelongs.Web.Services;

namespace ClarityBelongs.Tests;

public sealed class LearnSearchDiscoveryTests
{
    [Fact]
    public void Learn_catalog_covers_expanded_public_monitor_intents()
    {
        var catalog = new LearnContentCatalog();

        Assert.Equal(22, catalog.GetAll().Count);
        Assert.NotNull(catalog.GetBySlug("check-http-status-code-over-time"));
        Assert.NotNull(catalog.GetBySlug("monitor-website-redirect-destination"));
        Assert.NotNull(catalog.GetBySlug("monitor-a-broken-link"));
        Assert.NotNull(catalog.GetBySlug("nameserver-change-monitor"));
        Assert.NotNull(catalog.GetBySlug("monitor-mx-record-changes"));
        Assert.NotNull(catalog.GetBySlug("monitor-spf-record-changes"));
        Assert.NotNull(catalog.GetBySlug("monitor-dkim-record-changes"));
        Assert.NotNull(catalog.GetBySlug("monitor-dmarc-record-changes"));
        Assert.NotNull(catalog.GetBySlug("monitor-public-api-endpoint-uptime"));
        Assert.NotNull(catalog.GetBySlug("monitor-public-service-outage"));
    }

    [Fact]
    public void Learn_catalog_slugs_and_search_intents_are_unique()
    {
        var entries = new LearnContentCatalog().GetAll();

        Assert.Equal(
            entries.Count,
            entries
                .Select(entry => entry.Slug)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());

        Assert.Equal(
            entries.Count,
            entries
                .Select(entry => entry.SearchIntent)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
    }

    [Fact]
    public void Every_learn_entry_points_to_a_public_product()
    {
        var publicCatalog = new PublicClarityProductCatalog(new ClarityProductCatalog());

        foreach (var entry in new LearnContentCatalog().GetAll())
        {
            Assert.True(
                publicCatalog.IsPublic(entry.ProductSlug),
                $"Learn entry '{entry.Slug}' points to non-public product '{entry.ProductSlug}'.");
        }
    }

    [Fact]
    public void Expanded_learn_routes_are_indexable_by_seo_policy()
    {
        var products = new PublicClarityProductCatalog(new ClarityProductCatalog());
        var learn = new LearnContentCatalog();

        foreach (var entry in learn.GetAll())
        {
            var metadata = PublicSiteSeoPolicy.Resolve(
                $"/learn/{entry.Slug}",
                products,
                learn);

            Assert.True(metadata.Indexable);
            Assert.Equal(
                $"https://claritybelongs.com/learn/{entry.Slug}",
                metadata.CanonicalUrl);
        }
    }
}
