using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using System.Net;
using System.Text.Json;

namespace ClarityBelongs.Tests;

public sealed class DnsRecordObservationTests
{
    [Theory]
    [InlineData("example.com", "NS", "ns1.example.net.", "ns2.example.net.")]
    [InlineData("example.com", "MX", "10 mail1.example.net.", "20 mail2.example.net.")]
    [InlineData("example.com", "TXT", "v=spf1 include:first.example -all", "v=spf1 include:second.example -all")]
    [InlineData("selector._domainkey.example.com", "TXT", "v=DKIM1; p=FIRST", "v=DKIM1; p=SECOND")]
    [InlineData("_dmarc.example.com", "TXT", "v=DMARC1; p=none", "v=DMARC1; p=reject")]
    public async Task Record_monitor_baseline_no_change_change_and_failure(
        string host,
        string recordType,
        string firstValue,
        string changedValue)
    {
        var responses = new Queue<HttpResponseMessage>(
        [
            DnsResponse(0, firstValue),
            DnsResponse(0, firstValue),
            DnsResponse(0, changedValue),
            DnsResponse(2)
        ]);
        using var http = new HttpClient(
            new StubHttpMessageHandler((_, _) =>
                Task.FromResult(responses.Dequeue())));
        var adapter = new DnsRecordObservationAdapter(http);
        var target = new Target
        {
            PrimaryUri = host
        };
        var source = new SourceDefinition
        {
            AdapterType = AdapterTypes.DnsRecord,
            ConfigurationJson = JsonSerializer.Serialize(new
            {
                recordType
            })
        };

        var baseline = await adapter.ObserveAsync(target, source);
        var unchanged = await adapter.ObserveAsync(target, source);
        var changed = await adapter.ObserveAsync(target, source);
        var failed = await adapter.ObserveAsync(target, source);

        Assert.True(baseline.Success);
        Assert.Equal(baseline.Fingerprint, unchanged.Fingerprint);
        Assert.NotEqual(baseline.Fingerprint, changed.Fingerprint);
        Assert.False(failed.Success);
        Assert.Equal("dns_status", failed.ErrorCode);
    }

    [Fact]
    public async Task Dns_http_failure_is_reported()
    {
        using var http = new HttpClient(
            new StubHttpMessageHandler((_, _) =>
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));
        var adapter = new DnsRecordObservationAdapter(http);

        var result = await adapter.ObserveAsync(
            new Target { PrimaryUri = "example.com" },
            new SourceDefinition
            {
                AdapterType = AdapterTypes.DnsRecord,
                ConfigurationJson = "{\"recordType\":\"NS\"}"
            });

        Assert.False(result.Success);
        Assert.Equal("dns_http", result.ErrorCode);
    }

    private static HttpResponseMessage DnsResponse(
        int status,
        params string[] values)
    {
        var answer = values
            .Select(value => new
            {
                data = value
            })
            .ToArray();
        var json = JsonSerializer.Serialize(new
        {
            Status = status,
            Answer = answer
        });

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
    }
}
