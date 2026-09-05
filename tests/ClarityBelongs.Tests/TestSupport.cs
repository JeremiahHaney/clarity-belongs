using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Net;
using System.Net.Http;

namespace ClarityBelongs.Tests;

internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        handler(
            request,
            cancellationToken);
}

internal sealed class SequenceObservationAdapter(
    string adapterType,
    params Func<ObservationResult>[] results) : IObservationAdapter
{
    private readonly Queue<Func<ObservationResult>> _results = new(results);

    public string AdapterType { get; } = adapterType;
    public Func<Task>? BeforeResultAsync { get; set; }
    public int CallCount { get; private set; }

    public async Task<ObservationResult> ObserveAsync(
        Target target,
        SourceDefinition source,
        CancellationToken cancellationToken = default)
    {
        CallCount++;

        if (BeforeResultAsync is not null)
            await BeforeResultAsync();

        if (_results.Count == 0)
            throw new InvalidOperationException("No fake observation result remains.");

        return _results.Dequeue()();
    }
}

internal sealed class FakeDnsAddressResolver(
    params Func<IPAddress[]>[] results) : Belongs.Shared.Observation.IDnsAddressResolver
{
    private readonly Queue<Func<IPAddress[]>> _results = new(results);

    public Task<IPAddress[]> ResolveAsync(
        string host,
        CancellationToken cancellationToken = default)
    {
        if (_results.Count == 0)
            throw new InvalidOperationException("No fake DNS result remains.");

        return Task.FromResult(_results.Dequeue()());
    }
}

internal sealed class ObservationStatusInterceptor : SaveChangesInterceptor
{
    public List<string> Statuses { get; } = [];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(
            eventData,
            result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context is null)
            return;

        foreach (var entry in context.ChangeTracker.Entries<ObservationRun>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                Statuses.Add(entry.Entity.Status);
        }
    }
}

internal sealed class SqliteTestStore : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private SqliteTestStore(
        SqliteConnection connection,
        ClarityDbContext db)
    {
        _connection = connection;
        Db = db;
    }

    public ClarityDbContext Db { get; }

    public static async Task<SqliteTestStore> CreateAsync(
        SaveChangesInterceptor? interceptor = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ClarityDbContext>()
            .UseSqlite(connection);

        if (interceptor is not null)
            options.AddInterceptors(interceptor);

        var db = new ClarityDbContext(options.Options);
        await db.Database.EnsureCreatedAsync();

        return new SqliteTestStore(
            connection,
            db);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

internal sealed record SeededFollow(
    AppUser User,
    Workspace Workspace,
    Target Target,
    SourceDefinition Source,
    Follow Follow,
    AlertRule Rule);

internal static class TestData
{
    public static ObservationResult Success(
        string json,
        string summary = "Healthy",
        int? statusCode = 200) =>
        new(
            true,
            "Healthy",
            "application/json",
            json,
            summary,
            statusCode);

    public static ObservationResult Failure(
        string message = "Probe failed") =>
        new(
            false,
            "Down",
            "application/json",
            "{}",
            message,
            ErrorCode: "test_failure",
            ErrorMessage: message);

    public static async Task<SeededFollow> SeedFollowAsync(
        ClarityDbContext db,
        string adapterType = "Fake",
        string monitorType = "WebsiteUptime",
        string status = FollowStatuses.Active,
        long? ownerUserId = null,
        long? workspaceId = null)
    {
        AppUser user;

        if (ownerUserId is null)
        {
            user = new AppUser
            {
                Email = $"user-{Guid.NewGuid():N}@clarity.test",
                DisplayName = "Test User"
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        else
        {
            user = await db.Users.SingleAsync(x => x.Id == ownerUserId.Value);
        }

        Workspace workspace;

        if (workspaceId is null)
        {
            workspace = new Workspace
            {
                OwnerUserId = user.Id,
                Name = "Test Workspace"
            };

            db.Workspaces.Add(workspace);
            await db.SaveChangesAsync();
        }
        else
        {
            workspace = await db.Workspaces.SingleAsync(x => x.Id == workspaceId.Value);
        }

        var target = new Target
        {
            TargetType = "Website",
            CanonicalKey = $"{adapterType}:{Guid.NewGuid():N}",
            DisplayName = "Test Target",
            PrimaryUri = "https://93.184.216.34/health"
        };

        db.Targets.Add(target);
        await db.SaveChangesAsync();

        var source = new SourceDefinition
        {
            TargetId = target.Id,
            AdapterType = adapterType,
            ConfigurationJson = "{}"
        };

        db.SourceDefinitions.Add(source);
        await db.SaveChangesAsync();

        var follow = new Follow
        {
            WorkspaceId = workspace.Id,
            TargetId = target.Id,
            SourceDefinitionId = source.Id,
            MonitorType = monitorType,
            Name = "Test Follow",
            Status = status,
            CheckCadenceMinutes = 360,
            NextCheckAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };

        db.Follows.Add(follow);
        await db.SaveChangesAsync();

        var rule = new AlertRule
        {
            FollowId = follow.Id,
            RuleType = "AnyMeaningfulChange",
            MinimumSeverity = ChangeSeverities.Notice,
            IsEnabled = true
        };

        db.AlertRules.Add(rule);
        await db.SaveChangesAsync();

        return new SeededFollow(
            user,
            workspace,
            target,
            source,
            follow,
            rule);
    }

    public static async Task AgeSuccessfulRunsAsync(ClarityDbContext db)
    {
        var runs = await db.ObservationRuns
            .Where(x => x.Status == ObservationStatuses.Succeeded)
            .ToListAsync();

        foreach (var run in runs)
            run.CompletedAtUtc = DateTime.UtcNow.AddMinutes(-1);

        await db.SaveChangesAsync();
    }
}
