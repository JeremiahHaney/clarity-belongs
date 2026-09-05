using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClarityBelongs.Web.Data;

public sealed class DatabaseStorageOptions
{
    public string? Path { get; set; }
    public string? BackupDirectory { get; set; }
}

public sealed record DatabaseOperationalStatus(
    bool Ready,
    bool Reachable,
    bool SchemaCurrent,
    bool Writable,
    DateTime? LastBackupUtc,
    string? LastError);

public sealed class DatabaseRuntimeState
{
    private readonly object _gate = new();
    private DatabaseOperationalStatus _status =
        new(false, false, false, false, null, "Database startup has not completed.");

    public DatabaseOperationalStatus Get()
    {
        lock (_gate)
            return _status;
    }

    public void Set(DatabaseOperationalStatus status)
    {
        lock (_gate)
            _status = status;
    }
}

public sealed class DatabasePathProvider
{
    public const string EnvironmentVariable = "CLARITY_DB_PATH";
    public const string BackupEnvironmentVariable = "CLARITY_BACKUP_DIR";

    public DatabasePathProvider(
        IOptions<DatabaseStorageOptions> options,
        IHostEnvironment environment)
    {
        DatabasePath = ResolveDatabasePath(options.Value, environment);
        BackupDirectory = ResolveBackupDirectory(options.Value, DatabasePath);
    }

    public string DatabasePath { get; }
    public string BackupDirectory { get; }
    public string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = DatabasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared,
        ForeignKeys = true
    }.ToString();

    public void EnsureStorageDirectories()
    {
        var databaseDirectory = System.IO.Path.GetDirectoryName(DatabasePath)
            ?? throw new InvalidOperationException("The configured database path has no parent directory.");

        Directory.CreateDirectory(databaseDirectory);
        Directory.CreateDirectory(BackupDirectory);

        if (!OperatingSystem.IsWindows())
        {
            TrySetUnixMode(databaseDirectory);
            TrySetUnixMode(BackupDirectory);
        }
    }

    private static string ResolveDatabasePath(
        DatabaseStorageOptions options,
        IHostEnvironment environment)
    {
        var configured = Environment.GetEnvironmentVariable(EnvironmentVariable);

        if (string.IsNullOrWhiteSpace(configured))
            configured = options.Path;

        if (!string.IsNullOrWhiteSpace(configured))
            return System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(configured));

        if (environment.IsDevelopment())
        {
            return System.IO.Path.GetFullPath(
                System.IO.Path.Combine(environment.ContentRootPath, ".data", "clarity.db"));
        }

        var root = OperatingSystem.IsWindows()
            ? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
            : "/var/lib";

        return System.IO.Path.Combine(root, "ClarityBelongs", "Data", "clarity.db");
    }

    private static string ResolveBackupDirectory(
        DatabaseStorageOptions options,
        string databasePath)
    {
        var configured = Environment.GetEnvironmentVariable(BackupEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(configured))
            configured = options.BackupDirectory;

        if (!string.IsNullOrWhiteSpace(configured))
            return System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(configured));

        var parent = System.IO.Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("The configured database path has no parent directory.");

        return System.IO.Path.Combine(parent, "backups");
    }

    private static void TrySetUnixMode(string directory)
    {
        try
        {
            Directory.SetUnixFileMode(
                directory,
                UnixFileMode.UserRead
                    | UnixFileMode.UserWrite
                    | UnixFileMode.UserExecute
                    | UnixFileMode.GroupRead
                    | UnixFileMode.GroupExecute);
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

public sealed record DatabaseBackupResult(
    string BackupFileName,
    DateTime CreatedUtc,
    long LengthBytes);

public sealed class SqliteBackupService(
    DatabasePathProvider paths,
    DatabaseRuntimeState runtimeState)
{
    public async Task<DatabaseBackupResult> BackupAsync(
        CancellationToken cancellationToken = default)
    {
        paths.EnsureStorageDirectories();

        var createdUtc = DateTime.UtcNow;
        var fileName = $"clarity-{createdUtc:yyyyMMdd-HHmmss}-v0.7.0.db";
        var destinationPath = System.IO.Path.Combine(paths.BackupDirectory, fileName);

        await using var source = new SqliteConnection(paths.ConnectionString);
        await using var destination = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = destinationPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString());

        await source.OpenAsync(cancellationToken);
        await destination.OpenAsync(cancellationToken);
        source.BackupDatabase(destination);

        var length = new FileInfo(destinationPath).Length;
        var previous = runtimeState.Get();
        runtimeState.Set(previous with { LastBackupUtc = createdUtc });

        return new DatabaseBackupResult(fileName, createdUtc, length);
    }

    public DateTime? GetNewestBackupUtc()
    {
        if (!Directory.Exists(paths.BackupDirectory))
            return null;

        return Directory
            .EnumerateFiles(paths.BackupDirectory, "clarity-*.db")
            .Select(file => new FileInfo(file).LastWriteTimeUtc)
            .OrderByDescending(value => value)
            .Cast<DateTime?>()
            .FirstOrDefault();
    }
}

public sealed class DatabaseStartupService(
    ClarityDbContext db,
    DatabaseSchemaService legacySchema,
    DatabasePathProvider paths,
    SqliteBackupService backups,
    DatabaseRuntimeState runtimeState,
    ILogger<DatabaseStartupService> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        paths.EnsureStorageDirectories();

        try
        {
            await legacySchema.PrepareForMigrationsAsync(cancellationToken);
            await db.Database.MigrateAsync(cancellationToken);
            await legacySchema.EnsureMembershipRowsAsync(cancellationToken);

            var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
            var schemaCurrent = !pending.Any();
            var writable = await VerifyWritableAsync(cancellationToken);
            var newestBackup = backups.GetNewestBackupUtc();

            if (!schemaCurrent || !writable)
                throw new InvalidOperationException("Database migration completed but validation did not pass.");

            runtimeState.Set(
                new DatabaseOperationalStatus(
                    true,
                    true,
                    true,
                    true,
                    newestBackup,
                    null));
        }
        catch (Exception ex)
        {
            runtimeState.Set(
                new DatabaseOperationalStatus(
                    false,
                    false,
                    false,
                    false,
                    backups.GetNewestBackupUtc(),
                    ex.Message));

            logger.LogCritical(
                ex,
                "Clarity database startup failed. The application will not start against an unverified schema.");
            throw;
        }
    }

    private async Task<bool> VerifyWritableAsync(CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "CREATE TEMP TABLE IF NOT EXISTS __clarity_write_probe (Value INTEGER); DELETE FROM __clarity_write_probe; INSERT INTO __clarity_write_probe(Value) VALUES (1); DELETE FROM __clarity_write_probe;";
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
