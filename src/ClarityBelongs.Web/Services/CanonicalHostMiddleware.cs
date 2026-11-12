using Microsoft.AspNetCore.Http;

namespace ClarityBelongs.Web.Services;

/// <summary>
/// Consolidates the public www hostname onto the canonical HTTPS host.
/// Only the exact public alias is redirected; other hosts are left untouched.
/// </summary>
public sealed class CanonicalHostMiddleware
{
    private const string AliasHost = "www.claritybelongs.com";
    private const string CanonicalHost = "claritybelongs.com";

    private readonly RequestDelegate _next;

    public CanonicalHostMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!string.Equals(
                context.Request.Host.Host,
                AliasHost,
                StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var destination = "https://" + CanonicalHost
            + context.Request.PathBase
            + context.Request.Path
            + context.Request.QueryString;

        context.Response.StatusCode = StatusCodes.Status308PermanentRedirect;
        context.Response.Headers.Location = destination;
    }
}
