using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Observation;

public sealed class ObservationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ObservationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDueFollowsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Clarity observation worker cycle failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task RunDueFollowsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();
        var engine = scope.ServiceProvider.GetRequiredService<ObservationEngine>();
        var now = DateTime.UtcNow;

        var followIds = await db.Follows
            .Where(x => x.Status == FollowStatuses.Active)
            .Where(x => x.NextCheckAtUtc <= now)
            .OrderBy(x => x.NextCheckAtUtc)
            .Select(x => x.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var followId in followIds)
        {
            try
            {
                await engine.RunFollowAsync(followId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Follow {FollowId} failed to execute", followId);
            }
        }
    }
}
