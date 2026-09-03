using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Observation;

public sealed class ObservationEngine(
    ClarityDbContext db,
    IEnumerable<IObservationAdapter> adapters)
{
    public async Task RunFollowAsync(long followId, CancellationToken cancellationToken = default)
    {
        var follow = await db.Follows.FirstOrDefaultAsync(x => x.Id == followId, cancellationToken);
        if (follow is null || follow.Status != FollowStatuses.Active)
            return;

        var target = await db.Targets.FirstAsync(x => x.Id == follow.TargetId, cancellationToken);
        var source = await db.SourceDefinitions.FirstAsync(x => x.Id == follow.SourceDefinitionId, cancellationToken);
        var adapter = adapters.FirstOrDefault(x => x.AdapterType == source.AdapterType)
            ?? throw new InvalidOperationException($"No observation adapter is registered for {source.AdapterType}.");

        var recentRun = await db.ObservationRuns
            .Where(x => x.TargetId == target.Id)
            .Where(x => x.SourceDefinitionId == source.Id)
            .Where(x => x.Status == ObservationStatuses.Succeeded)
            .Where(x => x.CompletedAtUtc >= DateTime.UtcNow.AddSeconds(-30))
            .OrderByDescending(x => x.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (recentRun is not null)
        {
            AdvanceFollow(follow, recentRun.CompletedAtUtc ?? DateTime.UtcNow);
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
        var result = await adapter.ObserveAsync(target, source, cancellationToken);
        var completed = DateTime.UtcNow;

        run.CompletedAtUtc = completed;
        run.DurationMilliseconds = (long)(completed - started).TotalMilliseconds;
        run.HttpStatusCode = result.HttpStatusCode;
        run.ErrorCode = result.ErrorCode;
        run.ErrorMessage = result.ErrorMessage;
        run.Status = result.Success ? ObservationStatuses.Succeeded : ObservationStatuses.Failed;

        follow.LastCheckedAtUtc = completed;
        follow.NextCheckAtUtc = completed.AddMinutes(Math.Clamp(follow.CheckCadenceMinutes, 1, 10080));
        follow.UpdatedAtUtc = completed;

        if (!result.Success)
        {
            follow.Status = FollowStatuses.Error;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var previous = await db.Snapshots
            .Where(x => x.TargetId == target.Id)
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

        if (previous is not null && previous.Fingerprint != snapshot.Fingerprint)
        {
            var change = new Change
            {
                TargetId = target.Id,
                PreviousSnapshotId = previous.Id,
                CurrentSnapshotId = snapshot.Id,
                DetectedAtUtc = completed,
                ChangeType = source.AdapterType switch
                {
                    AdapterTypes.Dns => "DnsChanged",
                    AdapterTypes.Tls => "CertificateChanged",
                    AdapterTypes.Domain => "ExpirationChanged",
                    _ => "ContentChanged"
                },
                Severity = source.AdapterType is AdapterTypes.Tls or AdapterTypes.Domain
                    ? ChangeSeverities.Important
                    : ChangeSeverities.Notice,
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
                .Where(x => x.Status != FollowStatuses.Archived)
                .ToListAsync(cancellationToken);

            foreach (var affected in affectedFollows)
            {
                await LinkAndAlertAsync(affected, change, cancellationToken);
                affected.LastMeaningfulChangeAtUtc = completed;
            }
        }

        follow.Status = FollowStatuses.Active;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task LinkAndAlertAsync(Follow follow, Change change, CancellationToken cancellationToken)
    {
        var existing = await db.FollowChanges.FindAsync([follow.Id, change.Id], cancellationToken);
        if (existing is not null)
            return;

        var rule = await db.AlertRules
            .Where(x => x.FollowId == follow.Id && x.IsEnabled)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var matched = rule is null || rule.RuleType == "AnyMeaningfulChange";
        var followChange = new FollowChange
        {
            FollowId = follow.Id,
            ChangeId = change.Id,
            MatchedRuleId = matched ? rule?.Id : null,
            Relevance = matched ? "Alert" : "Relevant",
            CreatedAtUtc = DateTime.UtcNow
        };

        db.FollowChanges.Add(followChange);

        if (!matched)
            return;

        var workspace = await db.Workspaces.FirstAsync(x => x.Id == follow.WorkspaceId, cancellationToken);
        var dedupKey = $"follow:{follow.Id}:change:{change.Id}:inapp";
        if (await db.Notifications.AnyAsync(x => x.DedupKey == dedupKey, cancellationToken))
            return;

        db.Notifications.Add(new Notification
        {
            WorkspaceId = follow.WorkspaceId,
            UserId = workspace.OwnerUserId,
            FollowId = follow.Id,
            ChangeId = change.Id,
            Channel = "InApp",
            Status = "Sent",
            DedupKey = dedupKey,
            Subject = change.Title,
            BodySummary = change.Summary,
            CreatedAtUtc = DateTime.UtcNow,
            SentAtUtc = DateTime.UtcNow
        });
    }

    private static void AdvanceFollow(Follow follow, DateTime checkedAtUtc)
    {
        follow.LastCheckedAtUtc = checkedAtUtc;
        follow.NextCheckAtUtc = DateTime.UtcNow.AddMinutes(Math.Clamp(follow.CheckCadenceMinutes, 1, 10080));
        follow.UpdatedAtUtc = DateTime.UtcNow;
    }
}
