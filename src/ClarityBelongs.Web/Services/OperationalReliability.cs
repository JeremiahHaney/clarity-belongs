using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace ClarityBelongs.Web.Services;

public sealed class FollowExecutionCoordinator
{
    private readonly ConcurrentDictionary<long, byte> _active = new();

    public bool TryClaim(
        long followId,
        out IDisposable claim)
    {
        if (!_active.TryAdd(followId, 0))
        {
            claim = NullClaim.Instance;
            return false;
        }

        claim = new Claim(
            followId,
            _active);
        return true;
    }

    private sealed class Claim(
        long followId,
        ConcurrentDictionary<long, byte> active) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                active.TryRemove(followId, out _);
        }
    }

    private sealed class NullClaim : IDisposable
    {
        public static NullClaim Instance { get; } = new();
        public void Dispose()
        {
        }
    }
}

public sealed record WorkerHeartbeat(
    string Name,
    bool Running,
    DateTime? StartedAtUtc,
    DateTime? LastHeartbeatUtc,
    DateTime? StoppedAtUtc);

public sealed class WorkerRuntimeState
{
    public static WorkerRuntimeState Current { get; } = new();

    private readonly ConcurrentDictionary<string, MutableHeartbeat> _workers = new();

    public void Started(string name)
    {
        var now = DateTime.UtcNow;
        var state = _workers.GetOrAdd(name, _ => new MutableHeartbeat());

        lock (state)
        {
            state.Running = true;
            state.StartedAtUtc = now;
            state.LastHeartbeatUtc = now;
            state.StoppedAtUtc = null;
        }
    }

    public void Pulse(string name)
    {
        var state = _workers.GetOrAdd(name, _ => new MutableHeartbeat());

        lock (state)
            state.LastHeartbeatUtc = DateTime.UtcNow;
    }

    public void Stopped(string name)
    {
        var state = _workers.GetOrAdd(name, _ => new MutableHeartbeat());
        var now = DateTime.UtcNow;

        lock (state)
        {
            state.Running = false;
            state.LastHeartbeatUtc = now;
            state.StoppedAtUtc = now;
        }
    }

    public WorkerHeartbeat Get(string name)
    {
        var state = _workers.GetOrAdd(name, _ => new MutableHeartbeat());

        lock (state)
        {
            return new WorkerHeartbeat(
                name,
                state.Running,
                state.StartedAtUtc,
                state.LastHeartbeatUtc,
                state.StoppedAtUtc);
        }
    }

    public IReadOnlyList<WorkerHeartbeat> GetAll() =>
        _workers.Keys
            .OrderBy(x => x, StringComparer.Ordinal)
            .Select(Get)
            .ToArray();

    private sealed class MutableHeartbeat
    {
        public bool Running { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? LastHeartbeatUtc { get; set; }
        public DateTime? StoppedAtUtc { get; set; }
    }
}

public sealed record OperationalMetrics(
    int DueFollowCount,
    DateTime? OldestDueAtUtc,
    double? OldestDueAgeMinutes,
    int StaleObservationCount,
    int FailedObservationCount,
    int PendingNotificationCount,
    int FailedNotificationCount);

public sealed class OperationalMetricsService(ClarityDbContext db)
{
    public static readonly TimeSpan StaleObservationThreshold = TimeSpan.FromMinutes(15);

    public async Task<OperationalMetrics> GetAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var dueQuery = db.Follows
            .AsNoTracking()
            .Where(x => x.Status != FollowStatuses.Paused)
            .Where(x => x.Status != FollowStatuses.Archived)
            .Where(x => x.NextCheckAtUtc <= nowUtc);
        var dueCount = await dueQuery.CountAsync(cancellationToken);
        var oldestDue = await dueQuery
            .OrderBy(x => x.NextCheckAtUtc)
            .Select(x => (DateTime?)x.NextCheckAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var staleBefore = nowUtc - StaleObservationThreshold;
        var staleCount = await db.ObservationRuns
            .AsNoTracking()
            .CountAsync(
                x => (x.Status == ObservationStatuses.Running
                        || x.Status == ObservationStatuses.Queued)
                    && x.StartedAtUtc <= staleBefore,
                cancellationToken);
        var failedCount = await db.ObservationRuns
            .AsNoTracking()
            .CountAsync(
                x => x.Status == ObservationStatuses.Failed,
                cancellationToken);
        var pendingNotifications = await db.Notifications
            .AsNoTracking()
            .CountAsync(
                x => x.Channel == "Email"
                    && x.Status == "Pending",
                cancellationToken);
        var failedNotifications = await db.Notifications
            .AsNoTracking()
            .CountAsync(
                x => x.Channel == "Email"
                    && x.Status == "Failed",
                cancellationToken);

        return new OperationalMetrics(
            dueCount,
            oldestDue,
            oldestDue.HasValue
                ? Math.Max(0, (nowUtc - oldestDue.Value).TotalMinutes)
                : null,
            staleCount,
            failedCount,
            pendingNotifications,
            failedNotifications);
    }
}

public sealed class ObservationRecoveryService(
    ClarityDbContext db,
    ILogger<ObservationRecoveryService> logger)
{
    public async Task<int> RecoverStaleRunsAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var staleBefore = nowUtc - OperationalMetricsService.StaleObservationThreshold;
        var staleRuns = await db.ObservationRuns
            .Where(x => x.Status == ObservationStatuses.Running
                || x.Status == ObservationStatuses.Queued)
            .Where(x => x.StartedAtUtc <= staleBefore)
            .OrderBy(x => x.StartedAtUtc)
            .ToListAsync(cancellationToken);

        if (staleRuns.Count == 0)
            return 0;

        foreach (var run in staleRuns)
        {
            run.Status = ObservationStatuses.Failed;
            run.CompletedAtUtc = nowUtc;
            run.ErrorCode = "stale_recovery";
            run.ErrorMessage = "Observation was abandoned after the worker stopped before completion.";
        }

        var executionKeys = staleRuns
            .Select(x => new { x.TargetId, x.SourceDefinitionId })
            .Distinct()
            .ToArray();

        foreach (var key in executionKeys)
        {
            var follows = await db.Follows
                .Where(x => x.TargetId == key.TargetId)
                .Where(x => x.SourceDefinitionId == key.SourceDefinitionId)
                .Where(x => x.Status != FollowStatuses.Paused)
                .Where(x => x.Status != FollowStatuses.Archived)
                .ToListAsync(cancellationToken);

            foreach (var follow in follows)
            {
                if (follow.NextCheckAtUtc > nowUtc)
                    follow.NextCheckAtUtc = nowUtc;

                follow.UpdatedAtUtc = nowUtc;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Recovered {StaleRunCount} stale observation runs older than {ThresholdMinutes} minutes",
            staleRuns.Count,
            OperationalMetricsService.StaleObservationThreshold.TotalMinutes);

        return staleRuns.Count;
    }
}
