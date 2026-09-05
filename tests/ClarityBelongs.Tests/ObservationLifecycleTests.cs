using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Tests;

public sealed class ObservationLifecycleTests
{
    [Fact]
    public async Task Baseline_unchanged_and_change_create_expected_history_and_alerts()
    {
        var statuses = new ObservationStatusInterceptor();
        await using var store = await SqliteTestStore.CreateAsync(statuses);
        var seeded = await TestData.SeedFollowAsync(store.Db);
        var adapter = new SequenceObservationAdapter(
            "Fake",
            () => TestData.Success("{\"value\":1}", "Value 1"),
            () => TestData.Success("{\"value\":1}", "Value 1"),
            () => TestData.Success("{\"value\":2}", "Value 2"));
        adapter.BeforeResultAsync = async () =>
        {
            var running = await store.Db.ObservationRuns
                .OrderByDescending(x => x.Id)
                .Select(x => x.Status)
                .FirstAsync();
            Assert.Equal(ObservationStatuses.Running, running);
        };
        var engine = new ObservationEngine(
            store.Db,
            [adapter]);

        await engine.RunFollowAsync(seeded.Follow.Id);

        Assert.Single(await store.Db.Snapshots.ToListAsync());
        Assert.Empty(await store.Db.Changes.ToListAsync());
        Assert.Equal(FollowStatuses.Active, seeded.Follow.Status);

        await TestData.AgeSuccessfulRunsAsync(store.Db);
        await engine.RunFollowAsync(seeded.Follow.Id);

        Assert.Equal(2, await store.Db.Snapshots.CountAsync());
        Assert.Empty(await store.Db.Changes.ToListAsync());

        await TestData.AgeSuccessfulRunsAsync(store.Db);
        await engine.RunFollowAsync(seeded.Follow.Id);

        Assert.Equal(3, await store.Db.Snapshots.CountAsync());
        var change = Assert.Single(await store.Db.Changes.ToListAsync());
        var link = Assert.Single(await store.Db.FollowChanges.ToListAsync());
        Assert.Equal(change.Id, link.ChangeId);
        Assert.Equal(seeded.Follow.Id, link.FollowId);
        Assert.Equal(seeded.Rule.Id, link.MatchedRuleId);
        Assert.Equal("Alert", link.Relevance);
        Assert.Equal(2, await store.Db.Notifications.CountAsync());
        Assert.Contains(ObservationStatuses.Queued, statuses.Statuses);
        Assert.Contains(ObservationStatuses.Running, statuses.Statuses);
        Assert.Contains(ObservationStatuses.Succeeded, statuses.Statuses);
    }

    [Fact]
    public async Task Failure_and_recovery_transition_follow_and_queue_operational_notifications()
    {
        var statuses = new ObservationStatusInterceptor();
        await using var store = await SqliteTestStore.CreateAsync(statuses);
        var seeded = await TestData.SeedFollowAsync(store.Db);
        var adapter = new SequenceObservationAdapter(
            "Fake",
            () => TestData.Success("{\"value\":1}"),
            () => TestData.Failure("Connection refused"),
            () => TestData.Success("{\"value\":1}"));
        var engine = new ObservationEngine(
            store.Db,
            [adapter]);

        await engine.RunFollowAsync(seeded.Follow.Id);
        await TestData.AgeSuccessfulRunsAsync(store.Db);
        await engine.RunFollowAsync(seeded.Follow.Id);

        Assert.Equal(FollowStatuses.Error, seeded.Follow.Status);
        Assert.Equal(
            ObservationStatuses.Failed,
            await store.Db.ObservationRuns
                .OrderByDescending(x => x.Id)
                .Select(x => x.Status)
                .FirstAsync());
        Assert.Equal(2, await store.Db.Notifications.CountAsync());

        await engine.RunFollowAsync(seeded.Follow.Id);

        Assert.Equal(FollowStatuses.Active, seeded.Follow.Status);
        Assert.Equal(4, await store.Db.Notifications.CountAsync());
        Assert.Equal(2, await store.Db.Snapshots.CountAsync());
        Assert.Empty(await store.Db.Changes.ToListAsync());
        Assert.Contains(ObservationStatuses.Failed, statuses.Statuses);
    }

    [Fact]
    public async Task Unexpected_adapter_exception_is_persisted_as_failed_run()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var seeded = await TestData.SeedFollowAsync(store.Db);
        var adapter = new SequenceObservationAdapter(
            "Fake",
            () => throw new InvalidDataException("Unexpected adapter failure"));
        var engine = new ObservationEngine(
            store.Db,
            [adapter]);

        await engine.RunFollowAsync(seeded.Follow.Id);

        var run = Assert.Single(await store.Db.ObservationRuns.ToListAsync());
        Assert.Equal(ObservationStatuses.Failed, run.Status);
        Assert.Equal("adapter_exception", run.ErrorCode);
        Assert.Equal(FollowStatuses.Error, seeded.Follow.Status);
    }
}
