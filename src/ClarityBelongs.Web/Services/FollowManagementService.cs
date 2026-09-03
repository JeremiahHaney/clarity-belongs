using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Services;

public sealed record CreateFollowInput(
    string Name,
    string Target,
    string TargetType,
    string MonitorType,
    string AdapterType,
    string SourceConfigurationJson,
    string Importance,
    int CheckCadenceMinutes,
    string AlertRuleType);

public sealed record FollowDetailModel(
    Follow Follow,
    Target Target,
    SourceDefinition Source,
    IReadOnlyList<MyClarityChangeItem> Changes,
    IReadOnlyList<Notification> Notifications,
    IReadOnlyList<AlertRule> AlertRules);

public sealed class FollowManagementService(ClarityDbContext db)
{
    public async Task<long> CreateAsync(CreateFollowInput input, CancellationToken cancellationToken = default)
    {
        var workspace = await db.Workspaces
            .OrderBy(x => x.Id)
            .FirstAsync(cancellationToken);

        var normalizedTarget = input.Target.Trim();
        var canonicalKey = $"{input.AdapterType}:{input.SourceConfigurationJson}:{normalizedTarget.ToLowerInvariant()}";

        var target = await db.Targets
            .FirstOrDefaultAsync(x => x.CanonicalKey == canonicalKey, cancellationToken);

        if (target is null)
        {
            target = new Target
            {
                TargetType = input.TargetType,
                CanonicalKey = canonicalKey,
                DisplayName = input.Name.Trim(),
                PrimaryUri = normalizedTarget
            };

            db.Targets.Add(target);
            await db.SaveChangesAsync(cancellationToken);
        }

        var source = await db.SourceDefinitions
            .FirstOrDefaultAsync(
                x => x.TargetId == target.Id
                    && x.AdapterType == input.AdapterType
                    && x.ConfigurationJson == input.SourceConfigurationJson,
                cancellationToken);

        if (source is null)
        {
            source = new SourceDefinition
            {
                TargetId = target.Id,
                AdapterType = input.AdapterType,
                ConfigurationJson = input.SourceConfigurationJson
            };

            db.SourceDefinitions.Add(source);
            await db.SaveChangesAsync(cancellationToken);
        }

        var follow = new Follow
        {
            WorkspaceId = workspace.Id,
            TargetId = target.Id,
            SourceDefinitionId = source.Id,
            MonitorType = input.MonitorType,
            Name = input.Name.Trim(),
            Importance = input.Importance,
            CheckCadenceMinutes = Math.Clamp(input.CheckCadenceMinutes, 1, 10080),
            NextCheckAtUtc = DateTime.UtcNow
        };

        db.Follows.Add(follow);
        await db.SaveChangesAsync(cancellationToken);

        db.AlertRules.Add(new AlertRule
        {
            FollowId = follow.Id,
            RuleType = input.AlertRuleType,
            MinimumSeverity = ChangeSeverities.Notice
        });

        await db.SaveChangesAsync(cancellationToken);
        return follow.Id;
    }

    public async Task<FollowDetailModel?> GetAsync(long followId, CancellationToken cancellationToken = default)
    {
        var follow = await db.Follows.FirstOrDefaultAsync(x => x.Id == followId, cancellationToken);
        if (follow is null)
            return null;

        var target = await db.Targets.FirstAsync(x => x.Id == follow.TargetId, cancellationToken);
        var source = await db.SourceDefinitions.FirstAsync(x => x.Id == follow.SourceDefinitionId, cancellationToken);

        var links = await db.FollowChanges
            .Where(x => x.FollowId == follow.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var changeIds = links.Select(x => x.ChangeId).ToArray();
        var changes = await db.Changes
            .Where(x => changeIds.Contains(x.Id))
            .OrderByDescending(x => x.DetectedAtUtc)
            .ToListAsync(cancellationToken);

        var linkMap = links.ToDictionary(x => x.ChangeId);
        var changeItems = changes
            .Select(x => new MyClarityChangeItem(
                follow.Id,
                follow.Name,
                x.Id,
                x.DetectedAtUtc,
                x.Severity,
                x.Title,
                x.Summary,
                linkMap[x.Id].IsAcknowledged))
            .ToList();

        var notifications = await db.Notifications
            .Where(x => x.FollowId == follow.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        var rules = await db.AlertRules
            .Where(x => x.FollowId == follow.Id)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return new FollowDetailModel(follow, target, source, changeItems, notifications, rules);
    }

    public async Task SetPausedAsync(long followId, bool paused, CancellationToken cancellationToken = default)
    {
        var follow = await db.Follows.FirstOrDefaultAsync(x => x.Id == followId, cancellationToken);
        if (follow is null)
            return;

        follow.Status = paused ? FollowStatuses.Paused : FollowStatuses.Active;
        follow.NextCheckAtUtc = paused ? follow.NextCheckAtUtc : DateTime.UtcNow;
        follow.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(long followId, CancellationToken cancellationToken = default)
    {
        var follow = await db.Follows.FirstOrDefaultAsync(x => x.Id == followId, cancellationToken);
        if (follow is null)
            return;

        follow.Status = FollowStatuses.Archived;
        follow.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSettingsAsync(
        long followId,
        int cadenceMinutes,
        string importance,
        bool alertsEnabled,
        CancellationToken cancellationToken = default)
    {
        var follow = await db.Follows.FirstOrDefaultAsync(x => x.Id == followId, cancellationToken);
        if (follow is null)
            return;

        follow.CheckCadenceMinutes = Math.Clamp(cadenceMinutes, 1, 10080);
        follow.Importance = importance;
        follow.UpdatedAtUtc = DateTime.UtcNow;

        var rules = await db.AlertRules
            .Where(x => x.FollowId == followId)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
            rule.IsEnabled = alertsEnabled;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AcknowledgeAsync(long followId, long changeId, CancellationToken cancellationToken = default)
    {
        var link = await db.FollowChanges.FindAsync([followId, changeId], cancellationToken);
        if (link is null)
            return;

        link.IsAcknowledged = true;
        link.AcknowledgedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
