using ClarityBelongs.Web.Services;

namespace ClarityBelongs.Tests;

public sealed class PublicContentDifferentiationTests
{
    [Fact]
    public void Every_public_product_has_product_specific_editorial_content()
    {
        var products = new PublicClarityProductCatalog(new ClarityProductCatalog())
            .GetAll();

        Assert.Equal(15, products.Count);

        foreach (var product in products)
        {
            Assert.True(
                PublicProductContentCatalog.HasContent(product.Slug),
                $"Missing editorial content for public product '{product.Slug}'.");

            var content = PublicProductContentCatalog.Get(product.Slug);

            Assert.Equal(3, content.UseCases.Count);
            Assert.True(content.Faqs.Count >= 3);
            Assert.False(string.IsNullOrWhiteSpace(content.Limitation));
        }
    }

    [Fact]
    public void Public_product_editorial_sections_are_not_shared_boilerplate()
    {
        var products = new PublicClarityProductCatalog(new ClarityProductCatalog())
            .GetAll();

        Assert.Equal(
            products.Count,
            products
                .Select(product => PublicProductContentCatalog.Get(product.Slug).ObserveBody)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());

        Assert.Equal(
            products.Count,
            products
                .Select(product => PublicProductContentCatalog.Get(product.Slug).Limitation)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
    }

    [Fact]
    public void Every_learn_article_has_article_specific_editorial_content()
    {
        var entries = new LearnContentCatalog().GetAll();

        Assert.Equal(22, entries.Count);

        foreach (var entry in entries)
        {
            Assert.True(
                LearnEditorialContentCatalog.HasContent(entry.Slug),
                $"Missing editorial content for Learn article '{entry.Slug}'.");

            var content = LearnEditorialContentCatalog.Get(entry.Slug);

            Assert.True(content.Faqs.Count >= 3);
            Assert.False(string.IsNullOrWhiteSpace(content.SimpleAnswer));
            Assert.False(string.IsNullOrWhiteSpace(content.DetailBody));
        }
    }

    [Fact]
    public void Learn_simple_answers_are_unique()
    {
        var entries = new LearnContentCatalog().GetAll();

        Assert.Equal(
            entries.Count,
            entries
                .Select(entry => LearnEditorialContentCatalog.Get(entry.Slug).SimpleAnswer)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
    }
}
