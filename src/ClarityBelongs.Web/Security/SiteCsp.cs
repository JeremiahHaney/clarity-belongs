using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClarityBelongs.Web.Security;

public static class SiteCsp
{
    public const string ReportPath = "/csp/report";
    public const int MaxReportBytes = 8192;

    public static string NewNonce() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    public static string Build(string nonce, bool softwareBelongs)
    {
        var cdn = softwareBelongs ? " https://cdn.jsdelivr.net" : "";
        return string.Join("; ", new[]
        {
            "default-src 'self'",
            $"script-src 'self' 'nonce-{nonce}'{cdn}",
            $"style-src 'self' 'nonce-{nonce}'",
            "img-src 'self' data: blob: https:",
            "font-src 'self' data:",
            "connect-src 'self' wss: https:",
            "worker-src 'self' blob:",
            "frame-src 'self' https://checkout.stripe.com https://js.stripe.com",
            "object-src 'none'",
            "base-uri 'self'",
            "frame-ancestors 'none'",
            "form-action 'self' https://checkout.stripe.com",
            "report-uri /csp/report"
        });
    }

    public static async Task<IResult> HandleReportAsync(
        HttpContext context,
        ILoggerFactory loggerFactory)
    {
        var contentType = context.Request.ContentType ?? "";
        if (!contentType.StartsWith("application/csp-report", StringComparison.OrdinalIgnoreCase)
            && !contentType.StartsWith("application/reports+json", StringComparison.OrdinalIgnoreCase))
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

        if (context.Request.ContentLength > MaxReportBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        var buffer = new byte[MaxReportBytes + 1];
        var count = 0;
        while (count < buffer.Length)
        {
            var read = await context.Request.Body.ReadAsync(
                buffer.AsMemory(count, buffer.Length - count),
                context.RequestAborted);
            if (read == 0)
                break;
            count += read;
        }

        if (count > MaxReportBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        var logger = loggerFactory.CreateLogger("CspViolationReporting");
        foreach (var violation in ParseReports(buffer.AsSpan(0, count)))
            logger.LogWarning(
                "CSP violation directive={Directive} target={Target}",
                violation.Directive,
                violation.Target);

        return Results.NoContent();
    }

    public static IReadOnlyList<(string Directive, string Target)> ParseReports(ReadOnlySpan<byte> json)
    {
        try
        {
            using var document = JsonDocument.Parse(json.ToArray());
            var result = new List<(string, string)>();
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray().Take(8))
                {
                    if (GetString(item, "type") != "csp-violation"
                        || !item.TryGetProperty("body", out var body))
                        continue;
                    Add(result, body, "effectiveDirective", "blockedURL");
                }
            }
            else if (root.ValueKind == JsonValueKind.Object
                     && root.TryGetProperty("csp-report", out var report))
            {
                Add(result, report, "effective-directive", "blocked-uri");
            }

            return result;
        }
        catch (JsonException)
        {
            return Array.Empty<(string, string)>();
        }
    }

    private static void Add(
        List<(string, string)> result,
        JsonElement body,
        string directiveName,
        string targetName)
    {
        var directive = GetString(body, directiveName);
        if (string.IsNullOrWhiteSpace(directive)
            || directive.Length > 64
            || !Regex.IsMatch(directive, @"\A[a-zA-Z0-9 -]+\z", RegexOptions.CultureInvariant))
            return;

        var target = GetString(body, targetName);
        if (target is "inline" or "eval" or "data" or "blob" or "self")
        {
            result.Add((directive, target));
            return;
        }

        if (Uri.TryCreate(target, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && uri.IdnHost.Length <= 100
            && Regex.IsMatch(uri.IdnHost, @"\A[a-zA-Z0-9.-]+\z", RegexOptions.CultureInvariant))
        {
            result.Add((directive, uri.IdnHost));
            return;
        }

        result.Add((directive, "other"));
    }

    private static string? GetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

public sealed class SiteCspMiddleware
{
    private readonly RequestDelegate _next;

    public SiteCspMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var enforce = configuration.GetValue<bool>("Security:Csp:EnforceEnabled");
        var reportOnly = configuration.GetValue<bool>("Security:Csp:ReportOnlyEnabled");
        if (!enforce && !reportOnly)
        {
            await _next(context);
            return;
        }

        var nonce = SiteCsp.NewNonce();
        context.Items["CspNonce"] = nonce;
        context.Response.OnStarting(() =>
        {
            if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
            {
                var header = enforce
                    ? "Content-Security-Policy"
                    : "Content-Security-Policy-Report-Only";
                context.Response.Headers[header] = SiteCsp.Build(
                    nonce,
                    softwareBelongs: false);
                context.Response.Headers.CacheControl = "private, no-store";
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
