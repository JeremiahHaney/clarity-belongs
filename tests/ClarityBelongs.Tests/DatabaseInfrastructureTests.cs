using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClarityBelongs.Tests;

public sealed class DatabaseInfrastructureTests
{
    [Fact]
    public async Task FreshInstall_MigratesToCurrentSchemaAndIsIdempotent()
    {
        await using var harness = TestDatabaseHarness.Create();

        await harness.StartupAsync();
        await harness.StartupAsync();

        await using var db = harness.OpenContext();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        var pending = await db.Database.GetPendingMigrationsAsync();
        var indexes = await GetIndexNamesAsync(db);

        Assert.Single(applied);
        Assert.Contains(DatabaseSchemaService.BaselineMigrationId, applied);
        Assert.Empty(pending);
        Assert.Contains("IX_Users_Email", indexes);
        Assert.Contains("IX_Targets_CanonicalKey", indexes);
        Assert.Contains("IX_Notifications_DedupKey", indexes);
        Assert.True(harness.RuntimeState.Get().Ready);
    }

    [Fact]
    public async Task CurrentPreMigrationSchema_IsAdoptedWithoutDataLoss()
    {
        await using var harness = TestDatabaseHarness.Create();
        var expectedNextCheck = DateTime.UtcNow.AddHours(7).AddTicks(-DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond);

        await using (var legacy = harness.OpenContext())
        {
            await legacy.Database.EnsureCreatedAsync();
            await SeedContinuityDataAsync(legacy, expectedNextCheck);
        }

        await harness.StartupAsync();

        await using var db = harness.OpenContext();
        var user = await db.Users.SingleAsync(x => x.Email == "owner@clarity.test");
        var follow = await db.Follows.SingleAsync(x => x.Name == "Continuity Follow");
        var membership = await db.Memberships.SingleAsync();
        var applied = await db.Database.GetAppliedMigrationsAsync();

        Assert.Equal("Owner", user.DisplayName);
        Assert.Equal(expectedNextCheck, follow.NextCheckAtUtc);
        Assert.Equal(MembershipPlans.Free, membership.PlanCode);
        Assert.Contains(DatabaseSchemaService.BaselineMigrationId, applied);
    }

    [Fact]
    public async Task SupportedOlderSchema_IsUpgradedAndDataIsPreserved()
    {
        await using var harness = TestDatabaseHarness.Create();
        var expectedNextCheck = DateTime.UtcNow.AddHours(9).AddTicks(-DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond);

        await using (var legacy = harness.OpenContext())
        {
            await legacy.Database.EnsureCreatedAsync();
            await SeedContinuityDataAsync(legacy, expectedNextCheck);
            await legacy.Database.ExecuteSqlRawAsync("DROP TABLE Memberships;");
            await legacy.Database.ExecuteSqlRawAsync("DROP TABLE PasswordResetTokens;");
            await legacy.Database.ExecuteSqlRawAsync("DROP TABLE FeedbackSubmissions;");
            await legacy.Database.ExecuteSqlRawAsync("ALTER TABLE Users DROP COLUMN PasswordHash;");
            await legacy.Database.ExecuteSqlRawAsync("ALTER TABLE Users DROP COLUMN EmailVerified;");
        }

        await harness.StartupAsync();

        await using var db = harness.OpenContext();
        var follow = await db.Follows.SingleAsync(x => x.Name == "Continuity Follow");
        var user = await db.Users.SingleAsync(x => x.Email == "owner@clarity.test");
        var membership = await db.Memberships.SingleAsync();

        Assert.Equal(expectedNextCheck, follow.NextCheckAtUtc);
        Assert.Null(user.PasswordHash);
        Assert.False(user.EmailVerified);
        Assert.Equal(MembershipPlans.Free, membership.PlanCode);
    }

    [Fact]
    public async Task BackupRestore_RestoresUsersFollowsHistoryAndSchedule()
    {
        await using var harness = TestDatabaseHarness.Create();
        await harness.StartupAsync();
        var expectedNextCheck = DateTime.UtcNow.AddHours(5).AddTicks(-DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond);

        await using (var db = harness.OpenContext())
            await SeedContinuityDataAsync(db, expectedNextCheck);

        var backup = await harness.Backups.BackupAsync();

        await using (var db = harness.OpenContext())
        {
            var follow = await db.Follows.SingleAsync(x => x.Name == "Continuity Follow");
            follow.NextCheckAtUtc = expectedNextCheck.AddDays(3);
            db.Users.Add(new AppUser
            {
                Email = "after-backup@clarity.test",
                DisplayName = "After Backup"
            });
            await db.SaveChangesAsync();
        }

        await harness.Backups.RestoreAsync(backup.BackupFileName);
        await harness.StartupAsync();

        await using (var db = harness.OpenContext())
        {
            var follow = await db.Follows.SingleAsync(x => x.Name == "Continuity Follow");

            Assert.Equal(expectedNextCheck, follow.NextCheckAtUtc);
            Assert.False(await db.Users.AnyAsync(x => x.Email == "after-backup@clarity.test"));
            Assert.Single(db.ObservationRuns);
            Assert.Single(db.Snapshots);
            Assert.Single(db.Changes);
        }
    }

    [Fact]
    public async Task NewRuntimeAgainstSameDatabase_PreservesStateAcrossRestart()
    {
        var root = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"clarity-db-test-{Guid.NewGuid():N}");
        await using var first = TestDatabaseHarness.Create(root);
        await first.StartupAsync();
        var expectedNextCheck = DateTime.UtcNow.AddMinutes(123).AddTicks(-DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond);

        await using (var db = first.OpenContext())
            await SeedContinuityDataAsync(db, expectedNextCheck);

        await first.DisposeAsync();

        await using var restarted = TestDatabaseHarness.Create(root, ownsRoot: true);
        await restarted.StartupAsync();

        await using var restartedDb = restarted.OpenContext();
        var follow = await restartedDb.Follows.SingleAsync(x => x.Name == "Continuity Follow");

        Assert.Equal(expectedNextCheck, follow.NextCheckAtUtc);
        Assert.Single(restartedDb.Users);
        Assert.Single(restartedDb.ObservationRuns);
        Assert.Single(restartedDb.Snapshots);
        Assert.Single(restartedDb.Changes);
    }

    [Fact]
    public async Task StartupFailure_IsSurfacedAndRuntimeIsNotMarkedReady()
    {
        var root = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"clarity-db-failure-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        await using var harness = TestDatabaseHarness.Create(
            root,
            databasePath: root,
            ownsRoot: true);

        await Assert.ThrowsAnyAsync<Exception>(() => harness.StartupAsync());

        Assert.False(harness.RuntimeState.Get().Ready);
        Assert.False(harness.RuntimeState.Get().SchemaCurrent);
    }

    private static async Task SeedContinuityDataAsync(
        ClarityDbContext db,
        DateTime nextCheckAtUtc)
    {
        var user = new AppUser
        {
            Email = "owner@clarity.test",
            DisplayName = "Owner",
            PasswordHash = "test-hash",
            EmailVerified = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var workspace = new Workspace
        {
            OwnerUserId = user.Id,
            Name = "Owner Workspace"
        };
        db.Workspaces.Add(workspace);

        var target = new Target
        {
            TargetType = "Website",
            CanonicalKey = "https://example.test/",
            DisplayName = "Example",
            PrimaryUri = "https://example.test/"
        };
        db.Targets.Add(target);
        await db.SaveChangesAsync();

        var source = new SourceDefinition
        {
            TargetId = target.Id,
            AdapterType = AdapterTypes.Http,
            ConfigurationJson = "{\"mode\":\"content\"}"
        };
        db.SourceDefinitions.Add(source);
        await db.SaveChangesAsync();

        var follow = new Follow
        {
            WorkspaceId = workspace.Id,
            TargetId = target.Id,
            SourceDefinitionId = source.Id,
            MonitorType = "WebsiteChange",
            Name = "Continuity Follow",
            CheckCadenceMinutes = 360,
            NextCheckAtUtc = nextCheckAtUtc
        };
        db.Follows.Add(follow);

        var run = new ObservationRun
        {
            TargetId = target.Id,
            SourceDefinitionId = source.Id,
            Status = ObservationStatuses.Succeeded,
            CompletedAtUtc = DateTime.UtcNow
        };
        db.ObservationRuns.Add(run);
        await db.SaveChangesAsync();

        var snapshot = new Snapshot
        {
            TargetId = target.Id,
            ObservationRunId = run.Id,
            ObservedAtUtc = DateTime.UtcNow,
            Fingerprint = "abc123",
            NormalizedDataJson = "{\"value\":1}"
        };
        db.Snapshots.Add(snapshot);
        await db.SaveChangesAsync();

        run.SnapshotId = snapshot.Id;
        db.Changes.Add(new Change
        {
            TargetId = target.Id,
            CurrentSnapshotId = snapshot.Id,
            DetectedAtUtc = DateTime.UtcNow,
            Title = "Changed",
            Summary = "Test history survived."
        });
        await db.SaveChangesAsync();
    }

    private static async Task<HashSet<string>> GetIndexNamesAsync(ClarityDbContext db)
    {
        await db.Database.OpenConnectionAsync();

        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='index' AND name NOT LIKE 'sqlite_%';";
            await using var reader = await command.ExecuteReaderAsync();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (await reader.ReadAsync())
                names.Add(reader.GetString(0));

            return names;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}

internal sealed class TestDatabaseHarness : IAsyncDisposable
{
    private readonly string _root;
    private readonly bool _ownsRoot;

    private TestDatabaseHarness(
        string root,
        string databasePath,
        bool ownsRoot)
    {
        _root = root;
        _ownsRoot = ownsRoot;
        Directory.CreateDirectory(root);

        var environment = new TestHostEnvironment
        {
            EnvironmentName = Environments.Production,
            ApplicationName = "ClarityBelongs.Tests",
            ContentRootPath = root,
            ContentRootFileProvider = new NullFileProvider()
        };
        Paths = new DatabasePathProvider(
            Options.Create(new DatabaseStorageOptions
            {
                Path = databasePath,
                BackupDirectory = System.IO.Path.Combine(root, "backups")
            }),
            environment);
        RuntimeState = new DatabaseRuntimeState();
        Backups = new SqliteBackupService(Paths, RuntimeState);
    }

    public DatabasePathProvider Paths { get; }
    public DatabaseRuntimeState RuntimeState { get; }
    public SqliteBackupService Backups { get; }

    public static TestDatabaseHarness Create(
        string? root = null,
        string? databasePath = null,
        bool ownsRoot = true)
    {
        root ??= System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"clarity-db-test-{Guid.NewGuid():N}");
        databasePath ??= System.IO.Path.Combine(root, "clarity.db");
        return new TestDatabaseHarness(root, databasePath, ownsRoot);
    }

    public ClarityDbContext OpenContext()
    {
        var options = new DbContextOptionsBuilder<ClarityDbContext>()
            .UseSqlite(Paths.ConnectionString)
            .Options;
        return new ClarityDbContext(options);
    }

    public async Task StartupAsync()
    {
        await using var db = OpenContext();
        var schema = new DatabaseSchemaService(db);
        var startup = new DatabaseStartupService(
            db,
            schema,
            Paths,
            Backups,
            RuntimeState,
            NullLogger<DatabaseStartupService>.Instance);
        await startup.InitializeAsync();
    }

    public ValueTask DisposeAsync()
    {
        if (_ownsRoot && Directory.Exists(_root))
        {
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return ValueTask.CompletedTask;
    }
}

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "ClarityBelongs.Tests";
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
