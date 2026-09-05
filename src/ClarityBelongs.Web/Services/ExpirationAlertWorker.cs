using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClarityBelongs.Web.Services;

public sealed class ExpirationAlertWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpirationAlertWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            WorkerHealth.Registry.Mark("expiration");

            try
            {
                await EvaluateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Clarity expiration alert cycle failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task EvaluateAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();

        var follows = await db.Follows
            .Where(x => x.Status != FollowStatuses.Archived)
            .Where(x => x.Status != FollowStatuses.Paused)
            .Where(x => x.MonitorType == "SslExpiration" || x.MonitorType == "DomainExpiration")
            .ToListAsync(cancellationToken);

        foreach (var follow in follows)
        {
            var snapshot = await db.Snapshots
                .Where(x => x.TargetId == follow.TargetId)
                .OrderByDescending(x => x.ObservedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (snapshot is null)
                continue;

            if (!TryGetExpiration(snapshot.NormalizedDataJson, out var expiresUtc))
                continue;

            var days = (int)Math.Ceiling((expiresUtc - DateTime.UtcNow).TotalDays);
            var threshold = GetThreshold(days);

            if (threshold == 0)
                continue;

            var workspace = await db.Workspaces
                .FirstAsync(x => x.Id == follow.WorkspaceId, cancellationToken);
            var kind = follow.MonitorType == "SslExpiration" ? "SSL certificate" : "Domain";
            var subject = $"{kind} expires in {Math.Max(0, days)} day{(days == 1 ? string.Empty : "s")}";
            var body = $"{follow.Name}: {kind} expiration is {expiresUtc:u}.";
            var dateKey = expiresUtc.ToString("yyyyMMdd");

            await AddIfMissingAsync(
                db,
                follow,
                workspace.OwnerUserId,
                "InApp",
                "Sent",
                $"follow:{follow.Id}:expiry:{dateKey}:{threshold}:inapp",
                subject,
                body,
                cancellationToken);

            await AddIfMissingAsync(
                db,
                follow,
                workspace.OwnerUserId,
                "Email",
                "Pending",
                $"follow:{follow.Id}:expiry:{dateKey}:{threshold}:email",
                $"Clarity: {subject}",
                body,
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task AddIfMissingAsync(
        ClarityDbContext db,
        Follow follow,
        long userId,
        string channel,
        string status,
        string dedupKey,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        if (await db.Notifications.AnyAsync(x => x.DedupKey == dedupKey, cancellationToken))
            return;

        db.Notifications.Add(new Notification
        {
            WorkspaceId = follow.WorkspaceId,
            UserId = userId,
            FollowId = follow.Id,
            ChangeId = 0,
            Channel = channel,
            Status = status,
            DedupKey = dedupKey,
            Subject = subject,
            BodySummary = body,
            CreatedAtUtc = DateTime.UtcNow,
            SentAtUtc = status == "Sent" ? DateTime.UtcNow : null
        });
    }

    private static bool TryGetExpiration(string json, out DateTime expiresUtc)
    {
        expiresUtc = default;

        try
        {
            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("expiresUtc", out var value))
                return false;

            if (!DateTimeOffset.TryParse(value.GetString(), out var parsed))
                return false;

            expiresUtc = parsed.UtcDateTime;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static int GetThreshold(int days)
    {
        if (days < 0)
            return -1;

        if (days <= 1)
            return 1;

        if (days <= 7)
            return 7;

        if (days <= 14)
            return 14;

        if (days <= 30)
            return 30;

        return 0;
    }
}
