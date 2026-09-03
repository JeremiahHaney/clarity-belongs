using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Observation;

public sealed class ObservationEngine(
    ClarityDbContext db,
    IEnumerable<IObservationAdapter> adapters)
{
    public async Task RunFollowAsync(
        long followId,
        CancellationToken cancellationToken = default)
    {
        var follow = await db.Follows
            .FirstOrDefaultAsync(x => x.Id == followId, cancellationToken);

        if (follow is null
            || follow.Status == FollowStatuses.Paused
            || follow.Status == FollowStatuses.Archived)
        {
            return;
        }

        var previousFollowStatus = follow.Status;
        var target = await db.Targets
            .FirstAsync(x => x.Id == follow.TargetId, cancellationToken);
        var source = await db.SourceDefinitions
            .FirstAsync(x => x.Id == follow.SourceDefinitionId, cancellationToken);

        var adapter = adapters
            .FirstOrDefault(x => x.AdapterType == source.AdapterType)
            ?? throw new InvalidOperationException(
                $"No observation adapter is registered for {source.AdapterType}.");

        var recentRun = await db.ObservationRuns
            .Where(x => x.TargetId == target.Id)
            .Where(x => x.SourceDefinitionId == source.Id)
            .Where(x => x.Status == ObservationStatuses.Succeeded)
            .Where(x => x.CompletedAtUtc >= DateTime.UtcNow.AddSeconds(-30))
            .OrderByDescending(x => x.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (recentRun is not null)
        {
            AdvanceFollow(
                follow,
                recentRun.CompletedAtUtc ?? DateTime.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var run = new ObservationRun
        {
            TargetId = target.Id,
            SourceDefinitionId = source.Id,
            StartedAtUtc = DateTime.UtcNow,
            Status = ObservationStatuses.Running
        };

        db.ObservationRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        var started = DateTime.UtcNow;
        var result = await adapter.ObserveAsync(
            target,
            source,
            cancellationToken);
        var completed = DateTime.UtcNow;

        run.CompletedAtUtc = completed;
        run.DurationMilliseconds = (long)(completed - started).TotalMilliseconds;
        run.HttpStatusCode = result.HttpStatusCode;
        run.ErrorCode = result.ErrorCode;
        run.ErrorMessage = result.ErrorMessage;
        run.Status = result.Success
            ? ObservationStatuses.Succeeded
            : ObservationStatuses.Failed;

        follow.LastCheckedAtUtc = completed;
        follow.NextCheckAtUtc = completed.AddMinutes(
            Math.Clamp(follow.CheckCadenceMinutes, 1, 10080));
        follow.UpdatedAtUtc = completed;

        if (!result.Success)
        {
            follow.Status = FollowStatuses.Error;

            if (previousFollowStatus != FollowStatuses.Error)
            {
                await QueueOperationalNotificationsAsync(
                    follow,
                    "failure",
                    $"{follow.Name} needs attention",
                    result.ErrorMessage ?? result.Summary,
                    cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (previousFollowStatus == FollowStatuses.Error)
        {
            await QueueOperationalNotificationsAsync(
                follow,
                "recovery",
                $"{follow.Name} recovered",
                result.Summary,
                cancellationToken);
        }

        var previous = await db.Snapshots
            .Where(x => x.TargetId == target.Id)
            .Where(x => x.ObservationRunId != run.Id)
            .OrderByDescending(x => x.ObservedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var snapshot = new Snapshot
        {
            TargetId = target.Id,
            ObservationRunId = run.Id,
            ObservedAtUtc = completed,
            ContentType = result.ContentType,
            Fingerprint = result.Fingerprint,
            NormalizedDataJson = result.NormalizedDataJson,
            SummaryText = result.Summary
        };

        db.Snapshots.Add(snapshot);
        await db.SaveChangesAsync(cancellationToken);
        run.SnapshotId = snapshot.Id;

        if (previous is not null
            && previous.Fingerprint != snapshot.Fingerprint)
        {
            var change = new Change
            {
                TargetId = target.Id,
                PreviousSnapshotId = previous.Id,
                CurrentSnapshotId = snapshot.Id,
                DetectedAtUtc = completed,
                ChangeType = GetChangeType(follow, source),
                Severity = GetSeverity(follow, source),
                Title = $"{follow.Name} changed",
                Summary = $"Before: {previous.SummaryText ?? "unknown"} | After: {snapshot.SummaryText ?? "unknown"}",
                BeforeJson = previous.NormalizedDataJson,
                AfterJson = snapshot.NormalizedDataJson,
                IsMeaningful = true
            };

            db.Changes.Add(change);
            await db.SaveChangesAsync(cancellationToken);

            var affectedFollows = await db.Follows
                .Where(x => x.TargetId == target.Id)
                .Where(x => x.SourceDefinitionId == source.Id)
                .Where(x => x.Status != FollowStatuses.Archived)
                .ToListAsync(cancellationToken);

            foreach (var affected in affectedFollows)
            {
                await LinkAndAlertAsync(
                    affected,
                    change,
                    cancellationToken);
                affected.LastMeaningfulChangeAtUtc = completed;
            }
        }

        follow.Status = FollowStatuses.Active;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task LinkAndAlertAsync(
        Follow follow,
        Change change,
        CancellationToken cancellationToken)
    {
        var existing = await db.FollowChanges.FindAsync(
            [follow.Id, change.Id],
            cancellationToken);

        if (existing is not null)
            return;

        var rules = await db.AlertRules
            .Where(x => x.FollowId == follow.Id)
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var matchedRule = rules
            .FirstOrDefault(x => RuleMatches(x, change));

        var matched = matchedRule is not null;

        db.FollowChanges.Add(new FollowChange
        {
            FollowId = follow.Id,
            ChangeId = change.Id,
            MatchedRuleId = matchedRule?.Id,
            Relevance = matched ? "Alert" : "Relevant",
            CreatedAtUtc = DateTime.UtcNow
        });

        if (!matched)
            return;

        await QueueChangeNotificationsAsync(
            follow,
            change,
            cancellationToken);
    }

    private async Task QueueChangeNotificationsAsync(
        Follow follow,
        Change change,
        CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces
            .FirstAsync(x => x.Id == follow.WorkspaceId, cancellationToken);

        await AddNotificationIfMissingAsync(
            follow,
            workspace.OwnerUserId,
            change.Id,
            "InApp",
            "Sent",
            $"follow:{follow.Id}:change:{change.Id}:inapp",
            change.Title,
            change.Summary,
            cancellationToken);

        await AddNotificationIfMissingAsync(
            follow,
            workspace.OwnerUserId,
            change.Id,
            "Email",
            "Pending",
            $"follow:{follow.Id}:change:{change.Id}:email",
            $"Clarity: {change.Title}",
            change.Summary,
            cancellationToken);
    }

    private async Task QueueOperationalNotificationsAsync(
        Follow follow,
        string eventKind,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces
            .FirstAsync(x => x.Id == follow.WorkspaceId, cancellationToken);
        var eventKey = $"follow:{follow.Id}:{eventKind}:{DateTime.UtcNow:yyyyMMddHHmmss}";

        await AddNotificationIfMissingAsync(
            follow,
            workspace.OwnerUserId,
            0,
            "InApp",
            "Sent",
            $"{eventKey}:inapp",
            subject,
            body,
            cancellationToken);

        await AddNotificationIfMissingAsync(
            follow,
            workspace.OwnerUserId,
            0,
            "Email",
            "Pending",
            $"{eventKey}:email",
            $"Clarity: {subject}",
            body,
            cancellationToken);
    }

    private async Task AddNotificationIfMissingAsync(
        Follow follow,
        long userId,
        long changeId,
        string channel,
        string status,
        string dedupKey,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var exists = await db.Notifications
            .AnyAsync(x => x.DedupKey == dedupKey, cancellationToken);

        if (exists)
            return;

        db.Notifications.Add(new Notification
        {
            WorkspaceId = follow.WorkspaceId,
            UserId = userId,
            FollowId = follow.Id,
            ChangeId = changeId,
            Channel = channel,
            Status = status,
            DedupKey = dedupKey,
            Subject = subject,
            BodySummary = body,
            CreatedAtUtc = DateTime.UtcNow,
            SentAtUtc = status == "Sent"
                ? DateTime.UtcNow
                : null
        });
    }

    private static bool RuleMatches(
        AlertRule rule,
        Change change)
    {
        if (rule.RuleType != "AnyMeaningfulChange")
            return false;

        return SeverityRank(change.Severity)
            >= SeverityRank(rule.MinimumSeverity);
    }

    private static int SeverityRank(string severity) => severity switch
    {
        ChangeSeverities.Critical => 4,
        ChangeSeverities.Important => 3,
        ChangeSeverities.Notice => 2,
        _ => 1
    };

    private static string GetChangeType(
        Follow follow,
        SourceDefinition source) => follow.MonitorType switch
    {
        "WebsiteUptime" => "StatusChanged",
        "WebsiteChange" => "ContentChanged",
        "SslExpiration" => "CertificateChanged",
        "DomainExpiration" => "ExpirationChanged",
        "DnsChange" => "DnsChanged",
        _ => source.AdapterType switch
        {
            AdapterTypes.Dns => "DnsChanged",
            AdapterTypes.Tls => "CertificateChanged",
            AdapterTypes.Domain => "ExpirationChanged",
            _ => "ContentChanged"
        }
    };

    private static string GetSeverity(
        Follow follow,
        SourceDefinition source) => follow.MonitorType switch
    {
        "WebsiteUptime" => ChangeSeverities.Important,
        "SslExpiration" => ChangeSeverities.Important,
        "DomainExpiration" => ChangeSeverities.Important,
        "DnsChange" => ChangeSeverities.Important,
        _ => source.AdapterType is AdapterTypes.Tls or AdapterTypes.Domain
            ? ChangeSeverities.Important
            : ChangeSeverities.Notice
    };

    private static void AdvanceFollow(
        Follow follow,
        DateTime checkedAtUtc)
    {
        follow.LastCheckedAtUtc = checkedAtUtc;
        follow.NextCheckAtUtc = DateTime.UtcNow.AddMinutes(
            Math.Clamp(follow.CheckCadenceMinutes, 1, 10080));
        follow.UpdatedAtUtc = DateTime.UtcNow;
    }
}
