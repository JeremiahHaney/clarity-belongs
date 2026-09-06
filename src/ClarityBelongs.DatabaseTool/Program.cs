using System.Security.Cryptography;
using System.Text.Json;
using ClarityBelongs.DatabaseTool;
using ClarityBelongs.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

if (HasArgument(args, "--compare"))
	return await CompareReportsAsync(args);

var sourcePath = GetArgument(args, "--source");
if (string.IsNullOrWhiteSpace(sourcePath))
{
	Console.Error.WriteLine(
		"Usage: dotnet run --project src/ClarityBelongs.DatabaseTool -- --source <clarity.db> [--output <report.json>]\n" +
		"   or: dotnet run --project src/ClarityBelongs.DatabaseTool -- --compare --baseline <before.json> --candidate <after.json>");
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

var integrity = await ExecuteScalarAsync(
	db,
	"PRAGMA integrity_check;");
var foreignKeyViolations = await CountRowsAsync(
	db,
	"PRAGMA foreign_key_check;");
var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
var pendingMigrations = await db.Database.GetPendingMigrationsAsync();

var report = new DatabaseInventoryReport(
	DateTime.UtcNow,
	new DatabaseInventorySource(
		Path.GetFileName(sourcePath),
		new FileInfo(sourcePath).Length,
		File.GetLastWriteTimeUtc(sourcePath),
		await ComputeSha256Async(sourcePath)),
	new DatabaseInventoryValidation(
		reachable,
		integrity,
		foreignKeyViolations,
		!pendingMigrations.Any(),
		appliedMigrations.ToArray(),
		pendingMigrations.ToArray()),
	new DatabaseInventoryCounts(
		await db.Users.CountAsync(),
		await db.Workspaces.CountAsync(),
		await db.Memberships.CountAsync(),
		await db.PasswordResetTokens.CountAsync(),
		await db.Targets.CountAsync(),
		await db.SourceDefinitions.CountAsync(),
		await db.Follows.CountAsync(),
		await db.ObservationRuns.CountAsync(),
		await db.Snapshots.CountAsync(),
		await db.Changes.CountAsync(),
		await db.AlertRules.CountAsync(),
		await db.FollowChanges.CountAsync(),
		await db.Notifications.CountAsync(),
		await db.DigestDeliveryStates.CountAsync(),
		await db.StripeWebhookEvents.CountAsync(),
		await db.FeedbackSubmissions.CountAsync()));

var json = JsonSerializer.Serialize(
	report,
	JsonOptions());

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

	await File.WriteAllTextAsync(
		outputPath,
		json);
	Console.WriteLine(
		$"Clarity database inventory written to: {outputPath}");
}

return string.Equals(
		integrity,
		"ok",
		StringComparison.OrdinalIgnoreCase)
	&& foreignKeyViolations == 0
	&& !pendingMigrations.Any()
		? 0
		: 5;

static async Task<int> CompareReportsAsync(string[] values)
{
	var baselinePath = GetArgument(
		values,
		"--baseline");
	var candidatePath = GetArgument(
		values,
		"--candidate");

	if (string.IsNullOrWhiteSpace(baselinePath)
		|| string.IsNullOrWhiteSpace(candidatePath))
	{
		Console.Error.WriteLine(
			"Compare mode requires --baseline <before.json> and --candidate <after.json>.");
		return 2;
	}

	baselinePath = Path.GetFullPath(baselinePath);
	candidatePath = Path.GetFullPath(candidatePath);

	if (!File.Exists(baselinePath)
		|| !File.Exists(candidatePath))
	{
		Console.Error.WriteLine(
			"One or both inventory reports were not found.");
		return 3;
	}

	var baseline = JsonSerializer.Deserialize<DatabaseInventoryReport>(
		await File.ReadAllTextAsync(baselinePath),
		JsonOptions());
	var candidate = JsonSerializer.Deserialize<DatabaseInventoryReport>(
		await File.ReadAllTextAsync(candidatePath),
		JsonOptions());

	if (baseline is null
		|| candidate is null)
	{
		Console.Error.WriteLine(
			"One or both inventory reports could not be parsed.");
		return 4;
	}

	var mismatches = DatabaseInventoryReconciler.Compare(
		baseline.Counts,
		candidate.Counts);

	if (mismatches.Count == 0)
	{
		Console.WriteLine(
			"Clarity database reconciliation passed: every durable table count matches the baseline.");
		return 0;
	}

	Console.Error.WriteLine(
		"Clarity database reconciliation failed:");
	foreach (var mismatch in mismatches)
		Console.Error.WriteLine($"- {mismatch}");

	return 6;
}

static bool HasArgument(
	string[] values,
	string name) =>
	values.Any(value =>
		string.Equals(
			value,
			name,
			StringComparison.OrdinalIgnoreCase));

static string? GetArgument(
	string[] values,
	string name)
{
	for (var index = 0; index < values.Length - 1; index++)
	{
		if (string.Equals(
				values[index],
				name,
				StringComparison.OrdinalIgnoreCase))
		{
			return values[index + 1];
		}
	}

	return null;
}

static JsonSerializerOptions JsonOptions() =>
	new()
	{
		PropertyNameCaseInsensitive = true,
		WriteIndented = true
	};

static async Task<string?> ExecuteScalarAsync(
	ClarityDbContext db,
	string commandText)
{
	await db.Database.OpenConnectionAsync();
	try
	{
		await using var command = db.Database
			.GetDbConnection()
			.CreateCommand();
		command.CommandText = commandText;
		return Convert.ToString(
			await command.ExecuteScalarAsync());
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
		await using var command = db.Database
			.GetDbConnection()
			.CreateCommand();
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
