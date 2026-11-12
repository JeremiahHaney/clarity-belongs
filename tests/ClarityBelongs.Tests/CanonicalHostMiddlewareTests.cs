using ClarityBelongs.Web.Services;
using Microsoft.AspNetCore.Http;

namespace ClarityBelongs.Tests;

public sealed class CanonicalHostMiddlewareTests
{
    [Theory]
    [InlineData("/", "", "https://claritybelongs.com/")]
    [InlineData("/products/domain-expiration", "", "https://claritybelongs.com/products/domain-expiration")]
    [InlineData("/learn/dns-change-monitor", "?source=google", "https://claritybelongs.com/learn/dns-change-monitor?source=google")]
    public async Task WwwHostPermanentlyRedirectsToCanonicalHost(
        string path,
        string query,
        string expected)
    {
        var nextCalled = false;
        var middleware = new CanonicalHostMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("www.claritybelongs.com");
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status308PermanentRedirect, context.Response.StatusCode);
        Assert.Equal(expected, context.Response.Headers.Location.ToString());
        Assert.False(nextCalled);
    }

    [Theory]
    [InlineData("claritybelongs.com")]
    [InlineData("localhost")]
    [InlineData("other.example")]
    [InlineData("www.claritybelongs.com.evil.example")]
    public async Task OtherHostsContinueThroughPipeline(string host)
    {
        var nextCalled = false;
        var middleware = new CanonicalHostMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Request.Path = "/products";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status308PermanentRedirect, context.Response.StatusCode);
    }

    [Fact]
    public async Task WwwAliasRetainsPathBaseAndIgnoresHostPort()
    {
        var middleware = new CanonicalHostMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("WWW.CLARITYBELONGS.COM", 8080);
        context.Request.PathBase = "/prefix";
        context.Request.Path = "/about";
        context.Request.QueryString = new QueryString("?utm_source=test");

        await middleware.InvokeAsync(context);

        Assert.Equal(
            "https://claritybelongs.com/prefix/about?utm_source=test",
            context.Response.Headers.Location.ToString());
    }
}
