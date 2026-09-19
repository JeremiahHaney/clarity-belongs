using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClarityBelongs.Web.Services;

public static class AcquisitionEventTypes
{
    public const string Visit = "Visit";
    public const string SignupCompleted = "SignupCompleted";
    public const string LoginCompleted = "LoginCompleted";
    public const string FollowStarted = "FollowStarted";
    public const string FollowCreated = "FollowCreated";
    public const string DashboardOpened = "DashboardOpened";
}

public sealed class AcquisitionAnalyticsService(
    ClarityDbContext db)
{
    public const string VisitorCookieName = "clarity.acquisition";
    private static readonly TimeSpan VisitWindow = TimeSpan.FromMinutes(30);

    public async Task TrackPublicVisitAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        if (!HttpMethods.IsGet(context.Request.Method)
            || !IsPublicPath(context.Request.Path))
        {
            return;
        }

        var visitorId = EnsureVisitorId(context);
        var now = DateTime.UtcNow;
        var source = Clean(context.Request.Query["utm_source"], 100);
        var medium = Clean(context.Request.Query["utm_medium"], 100);
        var campaign = Clean(context.Request.Query["utm_campaign"], 150);
        var latest = await db.AcquisitionEvents
            .AsNoTracking()
            .Where(x => x.VisitorId == visitorId)
            .Where(x => x.EventType == AcquisitionEventTypes.Visit)
            .OrderByDescending(x => x.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var campaignChanged = HasCampaignChanged(
            latest,
            source,
            medium,
            campaign);

        if (latest is not null
            && now - latest.OccurredAtUtc < VisitWindow
            && !campaignChanged)
        {
            return;
        }

        var userId = TryGetUserId(context.User);
        var attribution = ResolveAttribution(
            latest,
            source,
            medium,
            campaign);

        db.AcquisitionEvents.Add(new AcquisitionEvent
        {
            VisitorId = visitorId,
            UserId = userId,
            EventType = AcquisitionEventTypes.Visit,
            Path = Clean(context.Request.Path.Value, 500),
            Source = attribution.Source,
            Medium = attribution.Medium,
            Campaign = attribution.Campaign,
            OccurredAtUtc = now
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task RecordSignupCompletedAsync(
        HttpContext context,
        long userId,
        CancellationToken cancellationToken = default) =>
        RecordAuthenticatedRequestAsync(
            context,
            AcquisitionEventTypes.SignupCompleted,
            userId,
            null,
            null,
            null,
            cancellationToken);

    public Task RecordLoginCompletedAsync(
        HttpContext context,
        long userId,
        CancellationToken cancellationToken = default) =>
        RecordAuthenticatedRequestAsync(
            context,
            AcquisitionEventTypes.LoginCompleted,
            userId,
            null,
            null,
            null,
            cancellationToken);

    public async Task RecordUserEventAsync(
        string eventType,
        long userId,
        long? workspaceId,
        string? productSlug = null,
        long? followId = null,
        CancellationToken cancellationToken = default)
    {
        var attribution = await LatestUserAttributionAsync(
            userId,
            cancellationToken);
        var visitorId = attribution?.VisitorId
            ?? $"user-{userId}";

        if (await IsDuplicateRecentEventAsync(
                visitorId,
                eventType,
                userId,
                productSlug,
                followId,
                cancellationToken))
        {
            return;
        }

        db.AcquisitionEvents.Add(new AcquisitionEvent
        {
            VisitorId = visitorId,
            UserId = userId,
            WorkspaceId = workspaceId,
            EventType = eventType,
            ProductSlug = Clean(productSlug, 100),
            FollowId = followId,
            Source = attribution?.Source ?? "direct",
            Medium = attribution?.Medium,
            Campaign = attribution?.Campaign,
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordAuthenticatedRequestAsync(
        HttpContext context,
        string eventType,
        long userId,
        long? workspaceId,
        string? productSlug,
        long? followId,
        CancellationToken cancellationToken)
    {
        var visitorId = EnsureVisitorId(context);
        var latest = await db.AcquisitionEvents
            .AsNoTracking()
            .Where(x => x.VisitorId == visitorId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        db.AcquisitionEvents.Add(new AcquisitionEvent
        {
            VisitorId = visitorId,
            UserId = userId,
            WorkspaceId = workspaceId,
            EventType = eventType,
            Path = Clean(context.Request.Path.Value, 500),
            ProductSlug = Clean(productSlug, 100),
            FollowId = followId,
            Source = latest?.Source ?? "direct",
            Medium = latest?.Medium,
            Campaign = latest?.Campaign,
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AcquisitionEvent?> LatestUserAttributionAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        return await db.AcquisitionEvents
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => x.Source != null)
            .OrderByDescending(x => x.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> IsDuplicateRecentEventAsync(
        string visitorId,
        string eventType,
        long userId,
        string? productSlug,
        long? followId,
        CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow - VisitWindow;

        return await db.AcquisitionEvents
            .AsNoTracking()
            .AnyAsync(
                x => x.VisitorId == visitorId
                    && x.UserId == userId
                    && x.EventType == eventType
                    && x.ProductSlug == productSlug
                    && x.FollowId == followId
                    && x.OccurredAtUtc >= cutoff,
                cancellationToken);
    }

    private static (string Source, string? Medium, string? Campaign) ResolveAttribution(
        AcquisitionEvent? latest,
        string? source,
        string? medium,
        string? campaign)
    {
        return (
            source ?? latest?.Source ?? "direct",
            medium ?? latest?.Medium,
            campaign ?? latest?.Campaign);
    }

    private static bool HasCampaignChanged(
        AcquisitionEvent? latest,
        string? source,
        string? medium,
        string? campaign)
    {
        if (source is null
            && medium is null
            && campaign is null)
        {
            return false;
        }

        return !string.Equals(
                latest?.Source,
                source ?? latest?.Source,
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                latest?.Medium,
                medium ?? latest?.Medium,
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                latest?.Campaign,
                campaign ?? latest?.Campaign,
                StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureVisitorId(HttpContext context)
    {
        var existing = context.Request.Cookies[VisitorCookieName];

        if (IsValidVisitorId(existing))
            return existing!;

        var visitorId = Guid.NewGuid()
            .ToString("N");

        context.Response.Cookies.Append(
            VisitorCookieName,
            visitorId,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !context.RequestServices
                    .GetRequiredService<IHostEnvironment>()
                    .IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(30),
                IsEssential = false
            });

        return visitorId;
    }

    private static bool IsValidVisitorId(string? value) =>
        value is { Length: 32 }
        && value.All(Uri.IsHexDigit);

    private static long? TryGetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return long.TryParse(
            value,
            out var userId)
            ? userId
            : null;
    }

    private static string? Clean(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = new string(
            value
                .Where(character => !char.IsControl(character))
                .ToArray())
            .Trim();

        if (cleaned.Length == 0)
            return null;

        return cleaned.Length <= maxLength
            ? cleaned
            : cleaned[..maxLength];
    }

    private static bool IsPublicPath(PathString path)
    {
        var value = path.Value ?? string.Empty;

        return value == "/"
            || value == "/products"
            || value.StartsWith(
                "/products/",
                StringComparison.OrdinalIgnoreCase)
            || value == "/learn"
            || value.StartsWith(
                "/learn/",
                StringComparison.OrdinalIgnoreCase)
            || value == "/pricing"
            || value == "/about"
            || value == "/support"
            || value == "/contact"
            || value == "/privacy"
            || value == "/terms"
            || value == "/signup"
            || value == "/login";
    }
}
