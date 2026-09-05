using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Services;
using System.Text.Json;

namespace ClarityBelongs.Tests;

public sealed class Phase1CatalogContractTests
{
    public static TheoryData<string, string, string?> Phase1Products => new()
    {
        { "website-uptime", AdapterTypes.Http, null },
        { "http-status", AdapterTypes.Http, null },
        { "redirect-chain", AdapterTypes.Http, null },
        { "broken-link", AdapterTypes.Http, null },
        { "ssl-expiration", AdapterTypes.Tls, null },
        { "domain-expiration", AdapterTypes.Domain, null },
        { "dns-change", AdapterTypes.Dns, null },
        { "nameserver-change", AdapterTypes.DnsRecord, "NS" },
        { "mx-record", AdapterTypes.DnsRecord, "MX" },
        { "spf-record", AdapterTypes.DnsRecord, "TXT" },
        { "dkim-record", AdapterTypes.DnsRecord, "TXT" },
        { "dmarc-record", AdapterTypes.DnsRecord, "TXT" },
        { "api-endpoint-uptime", AdapterTypes.Http, null },
        { "service-outage", AdapterTypes.Http, null }
    };

    [Theory]
    [MemberData(nameof(Phase1Products))]
    public void Phase1_product_routes_to_expected_shared_engine(
        string slug,
        string adapterType,
        string? recordType)
    {
        var catalog = new ClarityProductCatalog();
        var product = catalog.GetBySlug(slug);

        Assert.NotNull(product);
        Assert.Equal(adapterType, product.AdapterType);

        if (recordType is null)
            return;

        using var config = JsonDocument.Parse(product.SourceConfigurationJson);
        Assert.Equal(
            recordType,
            config.RootElement
                .GetProperty("recordType")
                .GetString());
    }
}
