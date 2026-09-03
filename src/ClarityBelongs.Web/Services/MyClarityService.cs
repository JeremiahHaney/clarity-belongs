using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Services;

public sealed record MyClarityFollowSummary(
    long FollowId,
    string Name,
    string MonitorType,
    string Status,
    string Importance,
    DateTime? LastCheckedAtUtc,
    DateTime NextCheckAtUtc,
    DateTime? LastMeaningfulChangeAtUtc,
    int UnacknowledgedChangeCount,
    string? LatestChangeTitle,
    string? LatestChangeSeverity);

public sealed record MyClarityChangeItem(
    long FollowId,
    string FollowName,
    long ChangeId,
    DateTime DetectedAtUtc,
    string Severity,
    string Title,
    string Summary,
    bool IsAcknowledged);

public sealed record MyClarityDashboard(
    IReadOnlyList<MyClarityFollowSummary> NeedsAttention,
    IReadOnlyList<MyClarityChangeItem> RecentChanges,
    IReadOnlyList<MyClarityFollowSummary> Following,
    IReadOnlyList<Notification> Notifications);

public sealed class MyClarityService(ClarityDbContext db)
{
    public async Task<MyClarityDashboard> GetAsync(long workspaceId, CancellationToken cancellationToken = default)
    {
        var follows = await db.Follows
            .Where(x => x.WorkspaceId == workspaceId)
            .Where(x => x.Status != FollowStatuses.Archived)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var followIds = follows.Select(x => x.Id).ToArray();
        var links = await db.FollowChanges
            .Where(x => followIds.Contains(x.FollowId))
            .ToListAsync(cancellationToken);

        var changeIds = links.Select(x => x.ChangeId).Distinct().ToArray();
        var changes = await db.Changes
            .Where(x => changeIds.Contains(x.Id))
            .OrderByDescending(x => x.DetectedAtUtc)
            .ToListAsync(cancellationToken);

        var changeMap = changes.ToDictionary(x => x.Id);
        var summaries = follows
            .Select(follow =>
            {
                var followLinks = links
                    .Where(x => x.FollowId == follow.Id)
                    .OrderByDescending(x => changeMap.TryGetValue(x.ChangeId, out var change) ? change.DetectedAtUtc : DateTime.MinValue)
                    .ToList();

                var latest = followLinks
                    .Select(x => changeMap.GetValueOrDefault(x.ChangeId))
                    .FirstOrDefault(x => x is not null);

                return new MyClarityFollowSummary(
                    follow.Id,
                    follow.Name,
                    follow.MonitorType,
                    follow.Status,
                    follow.Importance,
                    follow.LastCheckedAtUtc,
                    follow.NextCheckAtUtc,
                    follow.LastMeaningfulChangeAtUtc,
                    followLinks.Count(x => !x.IsAcknowledged),
                    latest?.Title,
                    latest?.Severity);
            })
            .ToList();

        var recentChanges = links
            .Select(link => new { Link = link, Change = changeMap.GetValueOrDefault(link.ChangeId) })
            .Where(x => x.Change is not null)
            .OrderByDescending(x => x.Change!.DetectedAtUtc)
            .Take(50)
            .Select(x => new MyClarityChangeItem(
                x.Link.FollowId,
                follows.First(f => f.Id == x.Link.FollowId).Name,
                x.Change!.Id,
                x.Change.DetectedAtUtc,
                x.Change.Severity,
                x.Change.Title,
                x.Change.Summary,
                x.Link.IsAcknowledged))
            .ToList();

        var notifications = await db.Notifications
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        var needsAttention = summaries
            .Where(x => x.Status is FollowStatuses.Error or FollowStatuses.NeedsAttention || x.UnacknowledgedChangeCount > 0)
            .ToList();

        return new MyClarityDashboard(needsAttention, recentChanges, summaries, notifications);
    }
}
