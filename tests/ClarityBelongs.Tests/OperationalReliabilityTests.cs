using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using ClarityBelongs.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClarityBelongs.Tests;

public sealed class OperationalReliabilityTests
{
    [Fact]
    public async Task StaleRunningRecoveryFailsRunAndReschedulesWithoutAlerts()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var seeded = await TestData.SeedFollowAsync(store.Db);
        var now = DateTime.UtcNow;
        seeded.Follow.NextCheckAtUtc = now.AddHours(1);
        store.Db.ObservationRuns.Add(new ObservationRun
        {
            TargetId = seeded.Target.Id,
            SourceDefinitionId = seeded.Source.Id,
            StartedAtUtc = now.AddMinutes(-20),
            Status = ObservationStatuses.Running
        });
        await store.Db.SaveChangesAsync();

        var recovery = new ObservationRecoveryService(
            store.Db,
            NullLogger<ObservationRecoveryService>.Instance);
        var recovered = await recovery.RecoverStaleRunsAsync(now);

        var run = await store.Db.ObservationRuns.SingleAsync();
        await store.Db.Entry(seeded.Follow).ReloadAsync();

        Assert.Equal(1, recovered);
        Assert.Equal(ObservationStatuses.Failed, run.Status);
        Assert.Equal("stale_recovery", run.ErrorCode);
        Assert.NotNull(run.CompletedAtUtc);
        Assert.True(seeded.Follow.NextCheckAtUtc <= now);
        Assert.Empty(await store.Db.Changes.ToListAsync());
        Assert.Empty(await store.Db.Notifications.ToListAsync());
    }

    [Fact]
    public void ProcessWideClaimPreventsDuplicateFollowExecution()
    {
        var first = new FollowExecutionCoordinator();
        var second = new FollowExecutionCoordinator();

        Assert.True(first.TryClaim(4242, out var firstClaim));
        Assert.False(second.TryClaim(4242, out var blockedClaim));
        blockedClaim.Dispose();
        firstClaim.Dispose();
        Assert.True(second.TryClaim(4242, out var afterRelease));
        afterRelease.Dispose();
    }

    [Fact]
    public async Task MoreThanFiftyDueFollowsRemainOrderedAndDrainWithoutStarvation()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var seeded = await TestData.SeedFollowAsync(store.Db);
        var now = DateTime.UtcNow;
        seeded.Follow.NextCheckAtUtc = now.AddMinutes(-100);

        for (var i = 1; i < 75; i++)
        {
            store.Db.Follows.Add(new Follow
            {
                WorkspaceId = seeded.Workspace.Id,
                TargetId = seeded.Target.Id,
                SourceDefinitionId = seeded.Source.Id,
                MonitorType = seeded.Follow.MonitorType,
                Name = $"Backlog {i}",
                Status = FollowStatuses.Active,
                CheckCadenceMinutes = 360,
                NextCheckAtUtc = now.AddMinutes(-100 + i)
            });
        }

        await store.Db.SaveChangesAsync();

        var metrics = await new OperationalMetricsService(store.Db)
            .GetAsync(now);
        var firstBatch = await ObservationWorker.GetDueFollowIdsAsync(
            store.Db,
            now);

        Assert.Equal(75, metrics.DueFollowCount);
        Assert.Equal(50, firstBatch.Count);
        Assert.Equal(seeded.Follow.Id, firstBatch[0]);

        var processed = await store.Db.Follows
            .Where(x => firstBatch.Contains(x.Id))
            .ToListAsync();

        foreach (var follow in processed)
            follow.NextCheckAtUtc = now.AddHours(1);

        await store.Db.SaveChangesAsync();

        var secondBatch = await ObservationWorker.GetDueFollowIdsAsync(
            store.Db,
            now);

        Assert.Equal(25, secondBatch.Count);
        Assert.Empty(firstBatch.Intersect(secondBatch));
        Assert.Equal(75, firstBatch.Concat(secondBatch).Distinct().Count());
    }

    [Fact]
    public async Task CancellationMarksRunningObservationRetryableInsteadOfLeavingItStuck()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var seeded = await TestData.SeedFollowAsync(store.Db, adapterType: "Blocking");
        var adapter = new BlockingObservationAdapter();
        var engine = new ObservationEngine(
            store.Db,
            [adapter],
            new FollowExecutionCoordinator());
        using var cancellation = new CancellationTokenSource();

        var runTask = engine.RunFollowAsync(
            seeded.Follow.Id,
            cancellation.Token);
        await adapter.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

        var run = await store.Db.ObservationRuns.SingleAsync();
        await store.Db.Entry(seeded.Follow).ReloadAsync();

        Assert.Equal(ObservationStatuses.Failed, run.Status);
        Assert.Equal("worker_cancelled", run.ErrorCode);
        Assert.NotNull(run.CompletedAtUtc);
        Assert.True(seeded.Follow.NextCheckAtUtc <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task PendingNotificationIsDeliveredByNewWorkerInstanceAfterRestart()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var seeded = await TestData.SeedFollowAsync(store.Db);
        store.Db.Memberships.Add(new Membership
        {
            UserId = seeded.User.Id,
            WorkspaceId = seeded.Workspace.Id,
            PlanCode = MembershipPlans.Personal,
            Status = MembershipStatuses.Active
        });
        store.Db.Notifications.Add(new Notification
        {
            WorkspaceId = seeded.Workspace.Id,
            UserId = seeded.User.Id,
            FollowId = seeded.Follow.Id,
            Channel = "Email",
            Status = "Pending",
            DedupKey = "restart-notification",
            Subject = "Operational test",
            BodySummary = "No sensitive content"
        });
        await store.Db.SaveChangesAsync();

        var sender = new RecordingEmailSender();
        using var provider = BuildProvider(store.Db, sender);
        _ = new NotificationDeliveryWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EmailOptions { DeliveryMode = "Immediate" }),
            NullLogger<NotificationDeliveryWorker>.Instance);
        var restartedWorker = new NotificationDeliveryWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EmailOptions { DeliveryMode = "Immediate" }),
            NullLogger<NotificationDeliveryWorker>.Instance);

        await restartedWorker.DeliverPendingAsync();

        var notification = await store.Db.Notifications.SingleAsync();
        Assert.Equal("Sent", notification.Status);
        Assert.Equal(1, sender.SendCount);
    }

    [Fact]
    public async Task ExpirationEvaluationIsIdempotentAcrossWorkerRestart()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var seeded = await TestData.SeedFollowAsync(
            store.Db,
            monitorType: "SslExpiration");
        var now = DateTime.UtcNow;
        var run = new ObservationRun
        {
            TargetId = seeded.Target.Id,
            SourceDefinitionId = seeded.Source.Id,
            StartedAtUtc = now.AddMinutes(-1),
            CompletedAtUtc = now,
            Status = ObservationStatuses.Succeeded
        };
        store.Db.ObservationRuns.Add(run);
        await store.Db.SaveChangesAsync();
        store.Db.Snapshots.Add(new Snapshot
        {
            TargetId = seeded.Target.Id,
            ObservationRunId = run.Id,
            ObservedAtUtc = now,
            Fingerprint = "expiry",
            NormalizedDataJson = $"{{\"expiresUtc\":\"{now.AddDays(7):O}\"}}"
        });
        await store.Db.SaveChangesAsync();

        using var provider = BuildProvider(store.Db, new RecordingEmailSender());
        var firstWorker = new ExpirationAlertWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ExpirationAlertWorker>.Instance);
        await firstWorker.EvaluateAsync(now);

        var restartedWorker = new ExpirationAlertWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ExpirationAlertWorker>.Instance);
        await restartedWorker.EvaluateAsync(now.AddMinutes(1));

        var notifications = await store.Db.Notifications.ToListAsync();
        Assert.Equal(2, notifications.Count);
        Assert.Equal(2, notifications.Select(x => x.DedupKey).Distinct().Count());
    }

    private static ServiceProvider BuildProvider(
        ClarityBelongs.Web.Data.ClarityDbContext db,
        IClarityEmailSender sender)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(sender);
        services.AddSingleton<IClarityEmailSender>(sender);
        return services.BuildServiceProvider();
    }

    private sealed class BlockingObservationAdapter : IObservationAdapter
    {
        public string AdapterType => "Blocking";
        public TaskCompletionSource Started { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<ObservationResult> ObserveAsync(
            Target target,
            SourceDefinition source,
            CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                cancellationToken);
            return TestData.Success("{\"ok\":true}");
        }
    }

    private sealed class RecordingEmailSender : IClarityEmailSender
    {
        public bool IsEnabled => true;
        public int SendCount { get; private set; }

        public Task SendAsync(
            string recipient,
            string subject,
            string textBody,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SendCount++;
            return Task.CompletedTask;
        }
    }
}
