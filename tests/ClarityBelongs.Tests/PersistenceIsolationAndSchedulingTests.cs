using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using ClarityBelongs.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClarityBelongs.Tests;

public sealed class PersistenceIsolationAndSchedulingTests
{
    [Fact]
    public async Task Follow_history_and_schedule_survive_host_recreation()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"clarity-tests-{Guid.NewGuid():N}.db");

        try
        {
            long followId;
            DateTime persistedNextRun;

            using (var first = await CreateHostAsync(
                path,
                () => TestData.Success("{\"state\":\"ok\"}")))
            {
                await using var scope = first.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<FollowManagementService>();
                var engine = scope.ServiceProvider.GetRequiredService<ObservationEngine>();
                var account = await CreateAccountAsync(db);

                followId = await service.CreateAsync(
                    account.User.Id,
                    account.Workspace.Id,
                    new CreateFollowInput(
                        "Persisted Follow",
                        "https://93.184.216.34/health",
                        "Website",
                        "WebsiteUptime",
                        "Fake",
                        "{}",
                        "High",
                        360,
                        "AnyMeaningfulChange"));

                await engine.RunFollowAsync(followId);
                await TestData.AgeSuccessfulRunsAsync(db);

                persistedNextRun = await db.Follows
                    .Where(x => x.Id == followId)
                    .Select(x => x.NextCheckAtUtc)
                    .SingleAsync();

                await first.StopAsync();
            }

            using (var second = await CreateHostAsync(
                path,
                () => TestData.Success("{\"state\":\"ok\"}")))
            {
                await using var scope = second.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();
                var engine = scope.ServiceProvider.GetRequiredService<ObservationEngine>();
                var follow = await db.Follows.SingleAsync(x => x.Id == followId);

                Assert.Equal(persistedNextRun, follow.NextCheckAtUtc);
                Assert.Single(await db.Snapshots.ToListAsync());
                Assert.Single(await db.ObservationRuns.ToListAsync());

                await engine.RunFollowAsync(followId);

                Assert.Equal(2, await db.Snapshots.CountAsync());
                Assert.Equal(2, await db.ObservationRuns.CountAsync());

                await second.StopAsync();
            }
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task Cross_workspace_access_acknowledge_and_run_are_blocked()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var a = await CreateAccountAsync(store.Db);
        var b = await CreateAccountAsync(store.Db);
        var plans = new PlanCatalog();
        var memberships = new MembershipService(
            store.Db,
            plans);
        var service = new FollowManagementService(
            store.Db,
            memberships);
        var adapter = new SequenceObservationAdapter(
            "Fake",
            () => TestData.Success("{\"state\":1}"));
        var engine = new ObservationEngine(
            store.Db,
            [adapter]);
        var followB = await service.CreateAsync(
            b.User.Id,
            b.Workspace.Id,
            new CreateFollowInput(
                "B Follow",
                "https://93.184.216.34/b",
                "Website",
                "WebsiteUptime",
                "Fake",
                "{}",
                "High",
                360,
                "AnyMeaningfulChange"));

        Assert.Null(await service.GetAsync(a.Workspace.Id, followB));
        Assert.False(await engine.RunOwnedFollowAsync(a.Workspace.Id, followB));
        Assert.Equal(0, adapter.CallCount);
        Assert.True(await engine.RunOwnedFollowAsync(b.Workspace.Id, followB));
        Assert.Equal(1, adapter.CallCount);

        var snapshot = await store.Db.Snapshots.SingleAsync();
        var change = new Change
        {
            TargetId = snapshot.TargetId,
            CurrentSnapshotId = snapshot.Id,
            Title = "B change",
            Summary = "B only"
        };
        store.Db.Changes.Add(change);
        await store.Db.SaveChangesAsync();
        store.Db.FollowChanges.Add(new FollowChange
        {
            FollowId = followB,
            ChangeId = change.Id
        });
        await store.Db.SaveChangesAsync();

        await service.AcknowledgeAsync(
            a.Workspace.Id,
            followB,
            change.Id);

        var link = await store.Db.FollowChanges.SingleAsync();
        Assert.False(link.IsAcknowledged);
        Assert.Null(await service.GetAsync(a.Workspace.Id, followB));
        Assert.NotNull(await service.GetAsync(b.Workspace.Id, followB));
    }

    [Fact]
    public async Task Paused_resumed_and_archived_follows_have_correct_scheduler_eligibility()
    {
        await using var store = await SqliteTestStore.CreateAsync();
        var active = await TestData.SeedFollowAsync(store.Db);
        var paused = await TestData.SeedFollowAsync(
            store.Db,
            status: FollowStatuses.Paused);
        var archived = await TestData.SeedFollowAsync(
            store.Db,
            status: FollowStatuses.Archived);
        var now = DateTime.UtcNow;

        var due = await ObservationWorker.GetDueFollowIdsAsync(
            store.Db,
            now);

        Assert.Contains(active.Follow.Id, due);
        Assert.DoesNotContain(paused.Follow.Id, due);
        Assert.DoesNotContain(archived.Follow.Id, due);

        var memberships = new MembershipService(
            store.Db,
            new PlanCatalog());
        var service = new FollowManagementService(
            store.Db,
            memberships);

        await service.SetPausedAsync(
            paused.Workspace.Id,
            paused.Follow.Id,
            false);

        due = await ObservationWorker.GetDueFollowIdsAsync(
            store.Db,
            DateTime.UtcNow.AddSeconds(1));
        Assert.Contains(paused.Follow.Id, due);

        await service.ArchiveAsync(
            active.Workspace.Id,
            active.Follow.Id);

        due = await ObservationWorker.GetDueFollowIdsAsync(
            store.Db,
            DateTime.UtcNow.AddSeconds(1));
        Assert.DoesNotContain(active.Follow.Id, due);
    }

    private static async Task<IHost> CreateHostAsync(
        string databasePath,
        Func<ObservationResult> resultFactory)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddDbContext<ClarityDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));
        builder.Services.AddSingleton<PlanCatalog>();
        builder.Services.AddScoped<MembershipService>();
        builder.Services.AddScoped<FollowManagementService>();
        builder.Services.AddScoped<ObservationEngine>();
        builder.Services.AddScoped<IObservationAdapter>(_ =>
            new SequenceObservationAdapter(
                "Fake",
                resultFactory,
                resultFactory));

        var host = builder.Build();
        await host.StartAsync();

        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();
        await db.Database.EnsureCreatedAsync();

        return host;
    }

    private static async Task<(AppUser User, Workspace Workspace)> CreateAccountAsync(
        ClarityDbContext db)
    {
        var user = new AppUser
        {
            Email = $"account-{Guid.NewGuid():N}@clarity.test",
            DisplayName = "Account"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var workspace = new Workspace
        {
            OwnerUserId = user.Id,
            Name = "Workspace"
        };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        db.Memberships.Add(new Membership
        {
            UserId = user.Id,
            WorkspaceId = workspace.Id,
            PlanCode = MembershipPlans.Free,
            Status = MembershipStatuses.Free
        });
        await db.SaveChangesAsync();

        return (user, workspace);
    }
}
