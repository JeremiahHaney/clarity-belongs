using Belongs.Shared.Observation;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using System.Net;
using System.Net.Sockets;

namespace ClarityBelongs.Tests;

public sealed class DnsObservationTests
{
    private static readonly SourceDefinition Source = new()
    {
        AdapterType = AdapterTypes.Dns,
        ConfigurationJson = "{}"
    };

    [Fact]
    public async Task Address_set_is_normalized_and_order_independent()
    {
        var resolver = new FakeDnsAddressResolver(
            () =>
            [
                IPAddress.Parse("203.0.113.20"),
                IPAddress.Parse("203.0.113.10"),
                IPAddress.Parse("203.0.113.20")
            ]);
        var engine = new DnsObservationEngine(resolver);

        var values = await engine.ObserveAddressesAsync("https://Example.com./path");

        Assert.Equal(
            ["203.0.113.10", "203.0.113.20"],
            values);
    }

    [Fact]
    public async Task Baseline_unchanged_and_changed_sets_have_stable_fingerprints()
    {
        var resolver = new FakeDnsAddressResolver(
            () => [IPAddress.Parse("203.0.113.10"), IPAddress.Parse("203.0.113.20")],
            () => [IPAddress.Parse("203.0.113.20"), IPAddress.Parse("203.0.113.10")],
            () => [IPAddress.Parse("203.0.113.30")]);
        var adapter = new DnsObservationAdapter(
            new DnsObservationEngine(resolver));
        var target = new Target
        {
            PrimaryUri = "example.com"
        };

        var baseline = await adapter.ObserveAsync(target, Source);
        var unchanged = await adapter.ObserveAsync(target, Source);
        var changed = await adapter.ObserveAsync(target, Source);

        Assert.True(baseline.Success);
        Assert.Equal(baseline.Fingerprint, unchanged.Fingerprint);
        Assert.NotEqual(baseline.Fingerprint, changed.Fingerprint);
    }

    [Fact]
    public async Task Empty_address_set_is_down()
    {
        var resolver = new FakeDnsAddressResolver(() => []);
        var adapter = new DnsObservationAdapter(
            new DnsObservationEngine(resolver));

        var result = await adapter.ObserveAsync(
            new Target { PrimaryUri = "example.com" },
            Source);

        Assert.False(result.Success);
        Assert.Equal("dns_empty", result.ErrorCode);
    }

    [Fact]
    public async Task Resolver_failure_is_reported()
    {
        var resolver = new FakeDnsAddressResolver(() =>
            throw new SocketException((int)SocketError.HostNotFound));
        var adapter = new DnsObservationAdapter(
            new DnsObservationEngine(resolver));

        var result = await adapter.ObserveAsync(
            new Target { PrimaryUri = "example.com" },
            Source);

        Assert.False(result.Success);
        Assert.Equal("dns_error", result.ErrorCode);
    }
}
