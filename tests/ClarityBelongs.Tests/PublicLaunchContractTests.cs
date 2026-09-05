using ClarityBelongs.Web.Services;

namespace ClarityBelongs.Tests;

public sealed class PublicLaunchContractTests
{
    private static readonly string[] ExpectedPublicSlugs =
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

    private static readonly string[] PublicSourceFiles =
    [
        "src/ClarityBelongs.Web/Components/Pages/Home.razor",
        "src/ClarityBelongs.Web/Components/Pages/Products.razor",
        "src/ClarityBelongs.Web/Components/Pages/Pricing.razor",
        "src/ClarityBelongs.Web/Components/Pages/About.razor",
        "src/ClarityBelongs.Web/Components/Pages/Learn.razor",
        "src/ClarityBelongs.Web/Components/Pages/Support.razor",
        "src/ClarityBelongs.Web/Components/Pages/Contact.razor",
        "src/ClarityBelongs.Web/Components/Pages/Feedback.razor",
        "src/ClarityBelongs.Web/Components/Pages/Login.razor",
        "src/ClarityBelongs.Web/Components/Pages/Signup.razor",
        "src/ClarityBelongs.Web/Components/Pages/ForgotPassword.razor",
        "src/ClarityBelongs.Web/Components/Pages/ResetPassword.razor",
        "src/ClarityBelongs.Web/Components/Pages/Account.razor",
        "src/ClarityBelongs.Web/Components/Pages/Settings.razor",
        "src/ClarityBelongs.Web/Components/Pages/MyClarity.razor",
        "src/ClarityBelongs.Web/Components/Pages/FollowDetail.razor",
        "src/ClarityBelongs.Web/Components/Layout/MainLayout.razor",
        "src/ClarityBelongs.Web/Services/LearnContentCatalog.cs"
    ];

    private static readonly string[] ForbiddenPublicPhrases =
    [
        "Configure in Stripe",
        "release gate",
        "production later",
        "coming soon",
        "planned",
        "provider not configured",
        "environment configuration",
        "meaningful changes",
        "smart filtering",
        "V1",
        "testing"
    ];

    [Fact]
    public void Public_catalog_is_explicit_and_does_not_auto_publish_internal_products()
    {
        var catalog = new PublicClarityProductCatalog(new ClarityProductCatalog());
        var actual = catalog
            .GetAll()
            .Select(product => product.Slug)
            .OrderBy(slug => slug)
            .ToArray();
        var expected = ExpectedPublicSlugs
            .OrderBy(slug => slug)
            .ToArray();

        Assert.Equal(expected, actual);
        Assert.Null(catalog.GetBySlug("product-price"));
        Assert.Null(catalog.GetBySlug("definitely-not-a-product"));
    }

    [Fact]
    public void Public_catalog_copy_does_not_expose_development_or_filtering_claims()
    {
        var catalog = new PublicClarityProductCatalog(new ClarityProductCatalog());

        foreach (var product in catalog.GetAll())
        {
            var copy = string.Join(
                " ",
                product.Name,
                product.ShortDescription,
                product.Outcome,
                product.HelpText);

            foreach (var phrase in ForbiddenPublicPhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    copy,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Website_change_copy_describes_whole_page_comparison_without_smart_filtering_claims()
    {
        var catalog = new PublicClarityProductCatalog(new ClarityProductCatalog());
        var product = catalog.GetBySlug("website-change");

        Assert.NotNull(product);
        Assert.Contains("whole-page", product.Outcome, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("meaningful", product.Outcome, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("smart", product.Outcome, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Launch_pages_do_not_regress_to_development_state_language()
    {
        var root = FindRepositoryRoot();

        foreach (var relativePath in PublicSourceFiles)
        {
            var text = File.ReadAllText(Path.Combine(root, relativePath));

            foreach (var phrase in ForbiddenPublicPhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    text,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Navigation_has_desktop_tablet_and_mobile_launch_breakpoints()
    {
        var root = FindRepositoryRoot();
        var layout = File.ReadAllText(
            Path.Combine(root, "src/ClarityBelongs.Web/Components/Layout/MainLayout.razor"));

        Assert.Contains("site-nav-menu", layout, StringComparison.Ordinal);
        Assert.Contains("max-width: 1050px", layout, StringComparison.Ordinal);
        Assert.Contains("max-width: 820px", layout, StringComparison.Ordinal);
        Assert.Contains("max-width: 430px", layout, StringComparison.Ordinal);
        Assert.Contains("flex-direction: row", layout, StringComparison.Ordinal);
        Assert.Contains("width: min(320px, calc(100vw - 32px))", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Sitemap_contains_only_approved_product_routes()
    {
        var root = FindRepositoryRoot();
        var sitemap = File.ReadAllText(
            Path.Combine(root, "src/ClarityBelongs.Web/wwwroot/sitemap.xml"));
        var productRoutes = sitemap
            .Split("<loc>", StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(part => part.Split("</loc>", 2)[0])
            .Where(url => url.Contains("/products/", StringComparison.Ordinal))
            .Select(url => url[(url.LastIndexOf('/') + 1)..])
            .OrderBy(slug => slug)
            .ToArray();
        var expected = ExpectedPublicSlugs
            .OrderBy(slug => slug)
            .ToArray();

        Assert.Equal(expected, productRoutes);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ClarityBelongs.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate ClarityBelongs.slnx from the test output directory.");
    }
}
