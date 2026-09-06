using System.Security.Cryptography;
using System.Text.Json;
using ClarityBelongs.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var sourcePath = GetArgument(args, "--source");
if (string.IsNullOrWhiteSpace(sourcePath))
{
    Console.Error.WriteLine("Usage: dotnet run --project src/ClarityBelongs.DatabaseTool -- --source <clarity.db> [--output <report.json>]");
    return 2;
}

sourcePath = Path.GetFullPath(sourcePath);
if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"Source database not found: {sourcePath}");
    return 3;
}

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = sourcePath,
    Mode = SqliteOpenMode.ReadOnly,
    ForeignKeys = true
}.ToString();

var options = new DbContextOptionsBuilder<ClarityDbContext>()
    .UseSqlite(connectionString)
    .Options;

await using var db = new ClarityDbContext(options);
var reachable = await db.Database.CanConnectAsync();
if (!reachable)
{
    Console.Error.WriteLine("The source database could not be opened.");
    return 4;
}

var integrity = await ExecuteScalarAsync(db, "PRAGMA integrity_check;");
var foreignKeyViolations = await CountRowsAsync(db, "PRAGMA foreign_key_check;");
var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
var pendingMigrations = await db.Database.GetPendingMigrationsAsync();

var report = new
{
    GeneratedUtc = DateTime.UtcNow,
    Source = new
    {
        FileName = Path.GetFileName(sourcePath),
        LengthBytes = new FileInfo(sourcePath).Length,
        LastWriteUtc = File.GetLastWriteTimeUtc(sourcePath),
        Sha256 = await ComputeSha256Async(sourcePath)
    },
    Validation = new
    {
        Reachable = reachable,
        Integrity = integrity,
        ForeignKeyViolations = foreignKeyViolations,
        SchemaCurrent = !pendingMigrations.Any(),
        AppliedMigrations = appliedMigrations.ToArray(),
        PendingMigrations = pendingMigrations.ToArray()
    },
    Counts = new
    {
        Users = await db.Users.CountAsync(),
        Workspaces = await db.Workspaces.CountAsync(),
        Memberships = await db.Memberships.CountAsync(),
        PasswordResetTokens = await db.PasswordResetTokens.CountAsync(),
        Targets = await db.Targets.CountAsync(),
        SourceDefinitions = await db.SourceDefinitions.CountAsync(),
        Follows = await db.Follows.CountAsync(),
        ObservationRuns = await db.ObservationRuns.CountAsync(),
        Snapshots = await db.Snapshots.CountAsync(),
        Changes = await db.Changes.CountAsync(),
        AlertRules = await db.AlertRules.CountAsync(),
        FollowChanges = await db.FollowChanges.CountAsync(),
        Notifications = await db.Notifications.CountAsync(),
        DigestDeliveryStates = await db.DigestDeliveryStates.CountAsync(),
        StripeWebhookEvents = await db.StripeWebhookEvents.CountAsync(),
        FeedbackSubmissions = await db.FeedbackSubmissions.CountAsync()
    }
};

var json = JsonSerializer.Serialize(
    report,
    new JsonSerializerOptions
    {
        WriteIndented = true
    });

var outputPath = GetArgument(args, "--output");
if (string.IsNullOrWhiteSpace(outputPath))
{
    Console.WriteLine(json);
}
else
{
    outputPath = Path.GetFullPath(outputPath);
    var parent = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(parent))
        Directory.CreateDirectory(parent);

    await File.WriteAllTextAsync(outputPath, json);
    Console.WriteLine($"Clarity database inventory written to: {outputPath}");
}

return string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase)
    && foreignKeyViolations == 0
    && !pendingMigrations.Any()
        ? 0
        : 5;

static string? GetArgument(string[] values, string name)
{
    for (var index = 0; index < values.Length - 1; index++)
    {
        if (string.Equals(values[index], name, StringComparison.OrdinalIgnoreCase))
            return values[index + 1];
    }

    return null;
}

static async Task<string?> ExecuteScalarAsync(
    ClarityDbContext db,
    string commandText)
{
    await db.Database.OpenConnectionAsync();
    try
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = commandText;
        return Convert.ToString(await command.ExecuteScalarAsync());
    }
    finally
    {
        await db.Database.CloseConnectionAsync();
    }
}

static async Task<int> CountRowsAsync(
    ClarityDbContext db,
    string commandText)
{
    await db.Database.OpenConnectionAsync();
    try
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = commandText;
        await using var reader = await command.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
            count++;
        return count;
    }
    finally
    {
        await db.Database.CloseConnectionAsync();
    }
}

static async Task<string> ComputeSha256Async(string path)
{
    await using var stream = File.OpenRead(path);
    var hash = await SHA256.HashDataAsync(stream);
    return Convert.ToHexString(hash);
}
