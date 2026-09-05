using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using System.Net;

namespace ClarityBelongs.Tests;

public sealed class DomainObservationTests
{
    private static readonly Target Target = new()
    {
        PrimaryUri = "example.com"
    };

    private static readonly SourceDefinition Source = new()
    {
        AdapterType = AdapterTypes.Domain,
        ConfigurationJson = "{}"
    };

    [Fact]
    public async Task Expiration_is_parsed_from_rdap()
    {
        using var http = HttpWithResponses(Rdap("2030-04-05T00:00:00Z"));
        var adapter = new DomainObservationAdapter(http);

        var result = await adapter.ObserveAsync(Target, Source);

        Assert.True(result.Success);
        Assert.Contains("2030-04-05", result.NormalizedDataJson);
    }

    [Fact]
    public async Task Changed_expiration_changes_fingerprint()
    {
        using var http = HttpWithResponses(
            Rdap("2030-04-05T00:00:00Z"),
            Rdap("2031-04-05T00:00:00Z"));
        var adapter = new DomainObservationAdapter(http);

        var first = await adapter.ObserveAsync(Target, Source);
        var second = await adapter.ObserveAsync(Target, Source);

        Assert.NotEqual(first.Fingerprint, second.Fingerprint);
    }

    [Fact]
    public async Task Missing_expiration_is_a_failed_observation()
    {
        using var http = HttpWithResponses("{\"events\":[]}");
        var adapter = new DomainObservationAdapter(http);

        var result = await adapter.ObserveAsync(Target, Source);

        Assert.False(result.Success);
        Assert.Equal("rdap_error", result.ErrorCode);
    }

    [Fact]
    public async Task Malformed_rdap_response_is_a_failed_observation()
    {
        using var http = HttpWithResponses("not-json");
        var adapter = new DomainObservationAdapter(http);

        var result = await adapter.ObserveAsync(Target, Source);

        Assert.False(result.Success);
        Assert.Equal("rdap_error", result.ErrorCode);
    }

    [Fact]
    public async Task Unsupported_registry_response_is_a_failed_observation()
    {
        using var http = new HttpClient(
            new StubHttpMessageHandler((_, _) =>
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.NotImplemented)
                    {
                        Content = new StringContent("unsupported")
                    })));
        var adapter = new DomainObservationAdapter(http);

        var result = await adapter.ObserveAsync(Target, Source);

        Assert.False(result.Success);
        Assert.Equal("rdap_error", result.ErrorCode);
    }

    private static HttpClient HttpWithResponses(params string[] bodies)
    {
        var queue = new Queue<string>(bodies);

        return new HttpClient(
            new StubHttpMessageHandler((_, _) =>
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(queue.Dequeue())
                    })));
    }

    private static string Rdap(string expiration) =>
        $"{{\"events\":[{{\"eventAction\":\"expiration\",\"eventDate\":\"{expiration}\"}}]}}";
}
