using System.Data.Common;
using System.Diagnostics;
using System.Security.Claims;
using ClarityBelongs.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Services;

public static class AdminAccess
{
    public static bool IsAllowed(
        ClaimsPrincipal principal,
        IConfiguration configuration)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.Identity?.Name;
        var allowed = configuration
            .GetSection("Admin:Emails")
            .Get<string[]>()
            ?? [];

        return principal.Identity?.IsAuthenticated == true
            && !string.IsNullOrWhiteSpace(email)
            && allowed.Any(configured =>
                string.Equals(
                    configured,
                    email,
                    StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record WebsiteRuntimeSnapshot(
    string MachineName,
    int ProcessId,
    double CpuPercent,
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    long GcMemoryBytes,
    int ThreadCount,
    int HandleCount,
    DateTime StartedUtc,
    TimeSpan Uptime);

public static class WebsiteRuntimeHealth
{
    private static readonly object CpuLock = new();
    private static DateTime _lastCpuSampleUtc = DateTime.UtcNow;
    private static TimeSpan _lastCpuTotal = Process.GetCurrentProcess().TotalProcessorTime;
    private static double _lastCpuPercent;

    public static WebsiteRuntimeSnapshot GetSnapshot()
    {
        var process = Process.GetCurrentProcess();
        process.Refresh();
        var startedUtc = process.StartTime.ToUniversalTime();

        return new WebsiteRuntimeSnapshot(
            Environment.MachineName,
            Environment.ProcessId,
            GetCpuPercent(process),
            process.WorkingSet64,
            process.PrivateMemorySize64,
            GC.GetTotalMemory(false),
            process.Threads.Count,
            process.HandleCount,
            startedUtc,
            DateTime.UtcNow - startedUtc);
    }

    private static double GetCpuPercent(Process process)
    {
        lock (CpuLock)
        {
            var now = DateTime.UtcNow;
            var cpu = process.TotalProcessorTime;
            var elapsedMs = (now - _lastCpuSampleUtc).TotalMilliseconds;

            if (elapsedMs < 250)
                return _lastCpuPercent;

            var cpuMs = (cpu - _lastCpuTotal).TotalMilliseconds;
            _lastCpuPercent = Math.Round(
                Math.Clamp(
                    cpuMs / elapsedMs / Environment.ProcessorCount * 100,
                    0,
                    100),
                1);
            _lastCpuSampleUtc = now;
            _lastCpuTotal = cpu;
            return _lastCpuPercent;
        }
    }
}

public sealed record DatabaseHealthSnapshot(
    bool Available,
    string Provider,
    string DatabaseName,
    string DataSource,
    decimal DatabaseSizeMb,
    int TableCount,
    int IndexCount,
    IReadOnlyList<DatabaseTableHealthRow> Tables,
    string? Error);

public sealed record DatabaseTableHealthRow(
    string Name,
    long Rows);

public sealed class DatabaseHealthService(ClarityDbContext db)
{
    public async Task<DatabaseHealthSnapshot> GetAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            var tables = await ReadTablesAsync(
                connection,
                cancellationToken);
            var indexes = await ScalarIntAsync(
                connection,
                "select count(*) from sqlite_master where type = 'index' and name not like 'sqlite_%';",
                cancellationToken);
            var pageCount = await ScalarLongAsync(
                connection,
                "pragma page_count;",
                cancellationToken);
            var pageSize = await ScalarLongAsync(
                connection,
                "pragma page_size;",
                cancellationToken);
            var sizeMb = Math.Round(
                pageCount * pageSize / 1024m / 1024m,
                2);

            return new DatabaseHealthSnapshot(
                true,
                db.Database.ProviderName ?? "Unknown",
                connection.Database,
                connection.DataSource,
                sizeMb,
                tables.Count,
                indexes,
                tables,
                null);
        }
        catch (Exception exception)
        {
            return new DatabaseHealthSnapshot(
                false,
                db.Database.ProviderName ?? "Unknown",
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                [],
                exception.Message);
        }
    }

    private static async Task<IReadOnlyList<DatabaseTableHealthRow>> ReadTablesAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        var names = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "select name from sqlite_master where type = 'table' and name not like 'sqlite_%' order by name;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
                names.Add(reader.GetString(0));
        }

        var rows = new List<DatabaseTableHealthRow>();
        foreach (var name in names)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"select count(*) from \"{name.Replace("\"", "\"\"")}\";";
            var count = Convert.ToInt64(
                await command.ExecuteScalarAsync(cancellationToken));
            rows.Add(new DatabaseTableHealthRow(name, count));
        }

        return rows
            .OrderByDescending(row => row.Rows)
            .ToArray();
    }

    private static async Task<int> ScalarIntAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<long> ScalarLongAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(
            await command.ExecuteScalarAsync(cancellationToken));
    }
}
