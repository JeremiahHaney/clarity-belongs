using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Observation;

public sealed class ObservationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ObservationWorker> logger) : BackgroundService
{
    public const string WorkerName = "observation";
    private const int BatchSize = 50;
    private readonly WorkerRuntimeState _runtimeState = WorkerRuntimeState.Current;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _runtimeState.Started(WorkerName);
        logger.LogInformation("Observation worker started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _runtimeState.Pulse(WorkerName);

                try
                {
                    await RunCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Observation worker cycle failed");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            _runtimeState.Stopped(WorkerName);
            logger.LogInformation("Observation worker stopped");
        }
    }

    internal async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        await RecoverStaleAsync(cancellationToken);

        var processed = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();
            var engine = scope.ServiceProvider.GetRequiredService<ObservationEngine>();
            var metrics = new OperationalMetricsService(db);
            var now = DateTime.UtcNow;
            var backlog = await metrics.GetAsync(now, cancellationToken);

            if (backlog.DueFollowCount > BatchSize
                || backlog.OldestDueAgeMinutes >= 5)
            {
                logger.LogWarning(
                    "Observation backlog has {DueFollowCount} due follows; oldest overdue age is {OldestDueAgeMinutes:F1} minutes",
                    backlog.DueFollowCount,
                    backlog.OldestDueAgeMinutes ?? 0);
            }

            var followIds = await GetDueFollowIdsAsync(
                db,
                now,
                cancellationToken);

            if (followIds.Count == 0)
                break;

            foreach (var followId in followIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                logger.LogDebug("Claimed due follow {FollowId} for observation", followId);

                try
                {
                    await engine.RunFollowAsync(
                        followId,
                        cancellationToken);
                    processed++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Follow {FollowId} failed to execute",
                        followId);
                }
            }

            if (followIds.Count < BatchSize)
                break;

            _runtimeState.Pulse(WorkerName);
            await Task.Yield();
        }

        if (processed > 0)
        {
            logger.LogInformation(
                "Observation worker processed {ProcessedFollowCount} due follows",
                processed);
        }
    }

    private async Task RecoverStaleAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();
        var recoveryLogger = scope.ServiceProvider
            .GetRequiredService<ILogger<ObservationRecoveryService>>();
        var recovery = new ObservationRecoveryService(
            db,
            recoveryLogger);
        await recovery.RecoverStaleRunsAsync(
            DateTime.UtcNow,
            cancellationToken);
    }

    internal static Task<List<long>> GetDueFollowIdsAsync(
        ClarityDbContext db,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return db.Follows
            .AsNoTracking()
            .Where(x => x.Status != FollowStatuses.Paused)
            .Where(x => x.Status != FollowStatuses.Archived)
            .Where(x => x.NextCheckAtUtc <= nowUtc)
            .OrderBy(x => x.NextCheckAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);
    }
}
