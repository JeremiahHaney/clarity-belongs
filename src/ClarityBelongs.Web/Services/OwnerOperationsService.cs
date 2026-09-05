using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClarityBelongs.Web.Services;

public static class OwnerAccess
{
    public static bool IsOwner(
        ClaimsPrincipal principal,
        IConfiguration configuration)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return false;

        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var ownerEmails = configuration
            .GetSection("Admin:Emails")
            .Get<string[]>()
            ?? [];

        return ownerEmails.Any(value =>
            string.Equals(
                value,
                email,
                StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record OwnerOverview(
    int TotalUsers,
    int ActiveFollows,
    int AttentionFollows,
    int FailedRuns24Hours,
    int PendingNotifications,
    int FailedNotifications,
    int RecentFeedback,
    int RecentContacts,
    int DueFollows,
    double? OldestDueAgeMinutes,
    DateTime? ObservationHeartbeatUtc,
    DateTime? ExpirationHeartbeatUtc,
    DateTime? NotificationHeartbeatUtc);

public sealed record OwnerUserRow(
    long UserId,
    string Email,
    string DisplayName,
    DateTime CreatedAtUtc,
    long WorkspaceId,
    string WorkspaceName,
    string PlanCode,
    string MembershipStatus,
    int ActiveFollowCount);

public sealed record OwnerFollowRow(
    long FollowId,
    string Name,
    string MonitorType,
    string Status,
    string Target,
    string AdapterType,
    string UserEmail,
    string WorkspaceName,
    int CadenceMinutes,
    DateTime? LastCheckedAtUtc,
    DateTime NextCheckAtUtc,
    string? LatestObservationStatus,
    DateTime? LatestObservationUtc,
    string? LatestErrorCode,
    string? LatestErrorMessage,
    IReadOnlyList<string> RecentChanges);

public sealed record OwnerFailedObservationRow(
    long RunId,
    long? FollowId,
    string AdapterType,
    string Target,
    string ErrorCategory,
    string ErrorSummary,
    DateTime TimestampUtc,
    string? UserEmail,
    string? WorkspaceName);

public sealed record OwnerNotificationRow(
    long Id,
    string Status,
    string Channel,
    string UserEmail,
    string Type,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc,
    DateTime? FailedAtUtc,
    string? LastError);

public sealed record OwnerMessageRow(
    string Source,
    long Id,
    string Type,
    string Message,
    string? Contact,
    DateTime CreatedUtc);

public sealed class OwnerOperationsService(ClarityDbContext db)
{
    public async Task<OwnerOverview> GetOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var since = now.AddHours(-24);
        var metrics = await new OperationalMetricsService(db)
            .GetAsync(now, cancellationToken);
        var runtime = WorkerRuntimeState.Current;

        return new OwnerOverview(
            await db.Users.CountAsync(cancellationToken),
            await db.Follows.CountAsync(
                x => x.Status == FollowStatuses.Active,
                cancellationToken),
            await db.Follows.CountAsync(
                x => x.Status == FollowStatuses.Error
                    || x.Status == FollowStatuses.NeedsAttention,
                cancellationToken),
            await db.ObservationRuns.CountAsync(
                x => x.Status == ObservationStatuses.Failed
                    && x.StartedAtUtc >= since,
                cancellationToken),
            await db.Notifications.CountAsync(
                x => x.Status == NotificationStatuses.Pending
                    || x.Status == NotificationStatuses.Sending,
                cancellationToken),
            await db.Notifications.CountAsync(
                x => x.Status == NotificationStatuses.Failed
                    || x.Status == NotificationStatuses.DeadLetter,
                cancellationToken),
            await db.FeedbackSubmissions.CountAsync(
                x => x.CreatedUtc >= since
                    && x.Kind != "contact",
                cancellationToken),
            await db.FeedbackSubmissions.CountAsync(
                x => x.CreatedUtc >= since
                    && x.Kind == "contact",
                cancellationToken),
            metrics.DueFollowCount,
            metrics.OldestDueAgeMinutes,
            runtime.Get("observation").LastHeartbeatUtc,
            runtime.Get("expiration").LastHeartbeatUtc,
            runtime.Get("notification").LastHeartbeatUtc);
    }

    public async Task<IReadOnlyList<OwnerUserRow>> SearchUsersAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var normalized = query?.Trim() ?? string.Empty;
        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            users = users.Where(x =>
                EF.Functions.Like(x.Email, $"%{normalized}%"));
        }

        return await (
            from user in users
            join workspace in db.Workspaces.AsNoTracking()
                on user.Id equals workspace.OwnerUserId
            join membership in db.Memberships.AsNoTracking()
                on workspace.Id equals membership.WorkspaceId into memberships
            from membership in memberships.DefaultIfEmpty()
            orderby user.CreatedAtUtc descending
            select new OwnerUserRow(
                user.Id,
                user.Email,
                user.DisplayName,
                user.CreatedAtUtc,
                workspace.Id,
                workspace.Name,
                membership == null ? MembershipPlans.Free : membership.PlanCode,
                membership == null ? MembershipStatuses.Free : membership.Status,
                db.Follows.Count(follow =>
                    follow.WorkspaceId == workspace.Id
                    && follow.Status == FollowStatuses.Active)))
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OwnerFollowRow>> SearchFollowsAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var normalized = query?.Trim() ?? string.Empty;
        long.TryParse(normalized, out var followId);

        var baseQuery =
            from follow in db.Follows.AsNoTracking()
            join target in db.Targets.AsNoTracking()
                on follow.TargetId equals target.Id
            join source in db.SourceDefinitions.AsNoTracking()
                on follow.SourceDefinitionId equals source.Id
            join workspace in db.Workspaces.AsNoTracking()
                on follow.WorkspaceId equals workspace.Id
            join user in db.Users.AsNoTracking()
                on workspace.OwnerUserId equals user.Id
            select new
            {
                Follow = follow,
                Target = target,
                Source = source,
                Workspace = workspace,
                User = user
            };

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            baseQuery = baseQuery.Where(x =>
                (followId > 0 && x.Follow.Id == followId)
                || EF.Functions.Like(x.Target.PrimaryUri, $"%{normalized}%")
                || EF.Functions.Like(x.Target.DisplayName, $"%{normalized}%")
                || EF.Functions.Like(x.User.Email, $"%{normalized}%"));
        }

        var rows = await baseQuery
            .OrderByDescending(x => x.Follow.UpdatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);
        var result = new List<OwnerFollowRow>();

        foreach (var row in rows)
        {
            var latest = await db.ObservationRuns
                .AsNoTracking()
                .Where(x => x.SourceDefinitionId == row.Follow.SourceDefinitionId)
                .Where(x => x.TargetId == row.Follow.TargetId)
                .OrderByDescending(x => x.StartedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            var changes = await (
                from link in db.FollowChanges.AsNoTracking()
                join change in db.Changes.AsNoTracking()
                    on link.ChangeId equals change.Id
                where link.FollowId == row.Follow.Id
                orderby change.DetectedAtUtc descending
                select $"{change.DetectedAtUtc:u} - {change.Title}")
                .Take(5)
                .ToListAsync(cancellationToken);

            result.Add(new OwnerFollowRow(
                row.Follow.Id,
                row.Follow.Name,
                row.Follow.MonitorType,
                row.Follow.Status,
                row.Target.PrimaryUri,
                row.Source.AdapterType,
                row.User.Email,
                row.Workspace.Name,
                row.Follow.CheckCadenceMinutes,
                row.Follow.LastCheckedAtUtc,
                row.Follow.NextCheckAtUtc,
                latest?.Status,
                latest?.StartedAtUtc,
                latest?.ErrorCode,
                SanitizeError(latest?.ErrorMessage),
                changes));
        }

        return result;
    }

    public async Task<IReadOnlyList<OwnerFailedObservationRow>> GetFailuresAsync(
        CancellationToken cancellationToken = default)
    {
        var runs = await (
            from run in db.ObservationRuns.AsNoTracking()
            join source in db.SourceDefinitions.AsNoTracking()
                on run.SourceDefinitionId equals source.Id
            join target in db.Targets.AsNoTracking()
                on run.TargetId equals target.Id
            where run.Status == ObservationStatuses.Failed
            orderby run.StartedAtUtc descending
            select new { run, source, target })
            .Take(100)
            .ToListAsync(cancellationToken);
        var result = new List<OwnerFailedObservationRow>();

        foreach (var row in runs)
        {
            var owner = await (
                from follow in db.Follows.AsNoTracking()
                join workspace in db.Workspaces.AsNoTracking()
                    on follow.WorkspaceId equals workspace.Id
                join user in db.Users.AsNoTracking()
                    on workspace.OwnerUserId equals user.Id
                where follow.SourceDefinitionId == row.run.SourceDefinitionId
                    && follow.TargetId == row.run.TargetId
                select new
                {
                    FollowId = follow.Id,
                    user.Email,
                    WorkspaceName = workspace.Name
                })
                .FirstOrDefaultAsync(cancellationToken);

            result.Add(new OwnerFailedObservationRow(
                row.run.Id,
                owner?.FollowId,
                row.source.AdapterType,
                row.target.PrimaryUri,
                string.IsNullOrWhiteSpace(row.run.ErrorCode)
                    ? "ObservationFailed"
                    : row.run.ErrorCode,
                SanitizeError(row.run.ErrorMessage)
                    ?? "The observation did not complete successfully.",
                row.run.StartedAtUtc,
                owner?.Email,
                owner?.WorkspaceName));
        }

        return result;
    }

    public async Task<IReadOnlyList<OwnerNotificationRow>> GetNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await (
            from notification in db.Notifications.AsNoTracking()
            join user in db.Users.AsNoTracking()
                on notification.UserId equals user.Id
            orderby notification.CreatedAtUtc descending
            select new OwnerNotificationRow(
                notification.Id,
                notification.Status,
                notification.Channel,
                user.Email,
                notification.Subject,
                notification.CreatedAtUtc,
                notification.SentAtUtc,
                notification.FailedAtUtc,
                SanitizeError(notification.FailureReason)))
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OwnerMessageRow>> GetMessagesAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await db.FeedbackSubmissions
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .Take(100)
            .Select(x => new OwnerMessageRow(
                x.Kind == "contact" ? "Contact" : "Feedback",
                x.Id,
                x.Kind,
                x.Message,
                x.Contact,
                x.CreatedUtc))
            .ToListAsync(cancellationToken);

        return messages;
    }

    private static string? SanitizeError(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var firstLine = value
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Trim();

        if (string.IsNullOrWhiteSpace(firstLine))
            return null;

        return firstLine.Length <= 300
            ? firstLine
            : firstLine[..300];
    }
}
