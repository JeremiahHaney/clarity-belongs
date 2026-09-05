using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace ClarityBelongs.Tests;

public sealed class HttpObservationTests
{
    private static readonly Target Target = new()
    {
        PrimaryUri = "https://93.184.216.34/start"
    };

    private static readonly SourceDefinition UptimeSource = new()
    {
        AdapterType = AdapterTypes.Http,
        ConfigurationJson = "{\"mode\":\"uptime\"}"
    };

    [Fact]
    public async Task Valid_url_captures_status_and_final_url()
    {
        using var http = CreateHttp((_, _) => Response(HttpStatusCode.OK));
        var adapter = CreateAdapter(http);

        var result = await adapter.ObserveAsync(
            Target,
            UptimeSource);

        Assert.True(result.Success);
        Assert.Equal("Healthy", result.Status);
        Assert.Equal(200, result.HttpStatusCode);

        using var json = JsonDocument.Parse(result.NormalizedDataJson);
        Assert.Equal(
            "https://93.184.216.34/start",
            json.RootElement.GetProperty("finalUrl").GetString());
    }

    [Fact]
    public async Task Unchanged_status_and_destination_keep_same_fingerprint()
    {
        using var http = CreateHttp((_, _) => Response(HttpStatusCode.OK));
        var adapter = CreateAdapter(http);

        var baseline = await adapter.ObserveAsync(
            Target,
            UptimeSource);
        var followUp = await adapter.ObserveAsync(
            Target,
            UptimeSource);

        Assert.Equal(baseline.Fingerprint, followUp.Fingerprint);
    }

    [Fact]
    public async Task Status_change_changes_fingerprint()
    {
        var responses = new Queue<HttpStatusCode>(
        [
            HttpStatusCode.OK,
            HttpStatusCode.NotFound
        ]);
        using var http = CreateHttp((_, _) => Response(responses.Dequeue()));
        var adapter = CreateAdapter(http);

        var baseline = await adapter.ObserveAsync(
            Target,
            UptimeSource);
        var changed = await adapter.ObserveAsync(
            Target,
            UptimeSource);

        Assert.True(baseline.Success);
        Assert.True(changed.Success);
        Assert.Equal("Down", changed.Status);
        Assert.NotEqual(baseline.Fingerprint, changed.Fingerprint);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, 404)]
    [InlineData(HttpStatusCode.InternalServerError, 500)]
    public async Task Http_error_status_is_observed_not_lost_as_transport_failure(
        HttpStatusCode statusCode,
        int expectedCode)
    {
        using var http = CreateHttp((_, _) => Response(statusCode));
        var adapter = CreateAdapter(http);

        var result = await adapter.ObserveAsync(
            Target,
            UptimeSource);

        Assert.True(result.Success);
        Assert.Equal("Down", result.Status);
        Assert.Equal(expectedCode, result.HttpStatusCode);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public async Task Redirect_destination_is_followed_and_captured()
    {
        var calls = 0;
        using var http = CreateHttp((request, _) =>
        {
            calls++;

            if (request.RequestUri!.AbsolutePath == "/start")
            {
                var redirect = Response(HttpStatusCode.Redirect);
                redirect.Headers.Location = new Uri("/final", UriKind.Relative);
                return redirect;
            }

            return Response(HttpStatusCode.OK);
        });
        var adapter = CreateAdapter(http);

        var result = await adapter.ObserveAsync(
            Target,
            UptimeSource);

        Assert.True(result.Success);
        Assert.Equal(2, calls);

        using var json = JsonDocument.Parse(result.NormalizedDataJson);
        Assert.Equal(
            "https://93.184.216.34/final",
            json.RootElement.GetProperty("finalUrl").GetString());
    }

    [Fact]
    public async Task Redirect_destination_change_changes_fingerprint()
    {
        var startRequests = 0;
        using var http = CreateHttp((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath != "/start")
                return Response(HttpStatusCode.OK);

            startRequests++;
            var redirect = Response(HttpStatusCode.Redirect);
            redirect.Headers.Location = new Uri(
                startRequests == 1 ? "/one" : "/two",
                UriKind.Relative);
            return redirect;
        });
        var adapter = CreateAdapter(http);

        var baseline = await adapter.ObserveAsync(
            Target,
            UptimeSource);
        var changed = await adapter.ObserveAsync(
            Target,
            UptimeSource);

        Assert.NotEqual(baseline.Fingerprint, changed.Fingerprint);

        using var json = JsonDocument.Parse(changed.NormalizedDataJson);
        Assert.Equal(
            "https://93.184.216.34/two",
            json.RootElement.GetProperty("finalUrl").GetString());
    }

    [Fact]
    public async Task Timeout_is_a_failed_observation()
    {
        using var http = CreateHttp((_, _) =>
            throw new TaskCanceledException("Timed out"));
        var adapter = CreateAdapter(http);

        var result = await adapter.ObserveAsync(
            Target,
            UptimeSource);

        Assert.False(result.Success);
        Assert.Equal("http_exception", result.ErrorCode);
    }

    [Fact]
    public async Task Dns_failure_is_a_failed_observation()
    {
        using var http = CreateHttp((_, _) =>
            throw new HttpRequestException(
                "Name or service not known",
                new SocketException((int)SocketError.HostNotFound)));
        var adapter = CreateAdapter(http);

        var result = await adapter.ObserveAsync(
            Target,
            UptimeSource);

        Assert.False(result.Success);
        Assert.Equal("http_exception", result.ErrorCode);
    }

    [Fact]
    public async Task Connection_refused_is_a_failed_observation()
    {
        using var http = CreateHttp((_, _) =>
            throw new HttpRequestException(
                "Connection refused",
                new SocketException((int)SocketError.ConnectionRefused)));
        var adapter = CreateAdapter(http);

        var result = await adapter.ObserveAsync(
            Target,
            UptimeSource);

        Assert.False(result.Success);
        Assert.Equal("http_exception", result.ErrorCode);
    }

    [Theory]
    [InlineData("not a url", "invalid_uri")]
    [InlineData("ftp://93.184.216.34/file", "http_exception")]
    public async Task Malformed_or_unsupported_target_fails_cleanly(
        string target,
        string expectedError)
    {
        using var http = CreateHttp((_, _) => Response(HttpStatusCode.OK));
        var adapter = CreateAdapter(http);
        var input = new Target
        {
            PrimaryUri = target
        };

        var result = await adapter.ObserveAsync(
            input,
            UptimeSource);

        Assert.False(result.Success);
        Assert.Equal(expectedError, result.ErrorCode);
    }

    private static HttpObservationAdapter CreateAdapter(HttpClient http) =>
        new(
            http,
            usePinnedTransport: false);

    private static HttpClient CreateHttp(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) =>
        new(
            new StubHttpMessageHandler((request, cancellationToken) =>
                Task.FromResult(handler(request, cancellationToken))));

    private static HttpResponseMessage Response(HttpStatusCode statusCode) =>
        new(statusCode)
        {
            Content = new StringContent("ok")
        };
}
