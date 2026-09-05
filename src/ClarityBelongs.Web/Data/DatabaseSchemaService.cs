using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace ClarityBelongs.Web.Data;

public sealed class DatabaseSchemaService(ClarityDbContext db)
{
    public const string BaselineMigrationId = "20260905183011_InitialClarityBaseline";
    private const string EfProductVersion = "10.0.0";

    public async Task PrepareForMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            if (!await HasLegacySchemaAsync(cancellationToken))
                return;

            if (await HasMigrationHistoryAsync(cancellationToken))
                return;

            await UpgradeLegacySchemaToBaselineAsync(cancellationToken);
            await StampBaselineMigrationAsync(cancellationToken);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public async Task EnsureMembershipRowsAsync(CancellationToken cancellationToken = default)
    {
        var workspaces = await db.Workspaces
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var workspace in workspaces)
        {
            if (await db.Memberships.AnyAsync(x => x.WorkspaceId == workspace.Id, cancellationToken))
                continue;

            db.Memberships.Add(new Membership
            {
                UserId = workspace.OwnerUserId,
                WorkspaceId = workspace.Id,
                PlanCode = MembershipPlans.Free,
                Status = MembershipStatuses.Free
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpgradeLegacySchemaToBaselineAsync(CancellationToken cancellationToken)
    {
        await AddColumnIfMissingAsync(
            "Users",
            "PasswordHash",
            "ALTER TABLE Users ADD COLUMN PasswordHash TEXT NULL;",
            cancellationToken);

        await AddColumnIfMissingAsync(
            "Users",
            "EmailVerified",
            "ALTER TABLE Users ADD COLUMN EmailVerified INTEGER NOT NULL DEFAULT 0;",
            cancellationToken);

        await ExecuteAsync(
            """
            CREATE TABLE IF NOT EXISTS Memberships (
                Id INTEGER NOT NULL CONSTRAINT PK_Memberships PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                WorkspaceId INTEGER NOT NULL,
                PlanCode TEXT NOT NULL,
                Status TEXT NOT NULL,
                StripeCustomerId TEXT NULL,
                StripeSubscriptionId TEXT NULL,
                StripePriceId TEXT NULL,
                CurrentPeriodEndUtc TEXT NULL,
                CancelAtPeriodEnd INTEGER NOT NULL DEFAULT 0,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );
            """,
            cancellationToken);

        await ExecuteAsync(
            """
            CREATE TABLE IF NOT EXISTS PasswordResetTokens (
                Id INTEGER NOT NULL CONSTRAINT PK_PasswordResetTokens PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                TokenHash TEXT NOT NULL,
                ExpiresAtUtc TEXT NOT NULL,
                UsedAtUtc TEXT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            """,
            cancellationToken);

        await ExecuteAsync(
            """
            CREATE TABLE IF NOT EXISTS FeedbackSubmissions (
                Id INTEGER NOT NULL CONSTRAINT PK_FeedbackSubmissions PRIMARY KEY AUTOINCREMENT,
                Kind TEXT NOT NULL,
                Message TEXT NOT NULL,
                ProductSlug TEXT NULL,
                Path TEXT NULL,
                Contact TEXT NULL,
                CreatedUtc TEXT NOT NULL
            );
            """,
            cancellationToken);

        await ExecuteAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_Memberships_UserId ON Memberships (UserId);",
            cancellationToken);
        await ExecuteAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_Memberships_WorkspaceId ON Memberships (WorkspaceId);",
            cancellationToken);
        await ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS IX_Memberships_StripeCustomerId ON Memberships (StripeCustomerId);",
            cancellationToken);
        await ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS IX_Memberships_StripeSubscriptionId ON Memberships (StripeSubscriptionId);",
            cancellationToken);
        await ExecuteAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_PasswordResetTokens_TokenHash ON PasswordResetTokens (TokenHash);",
            cancellationToken);
        await ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS IX_FeedbackSubmissions_CreatedUtc ON FeedbackSubmissions (CreatedUtc);",
            cancellationToken);
    }

    private async Task<bool> HasLegacySchemaAsync(CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type='table' AND name='Users');";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) == 1;
    }

    private async Task<bool> HasMigrationHistoryAsync(CancellationToken cancellationToken)
    {
        await using var tableCommand = db.Database.GetDbConnection().CreateCommand();
        tableCommand.CommandText =
            "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory');";
        var tableResult = await tableCommand.ExecuteScalarAsync(cancellationToken);

        if (Convert.ToInt32(tableResult) != 1)
            return false;

        await using var rowCommand = db.Database.GetDbConnection().CreateCommand();
        rowCommand.CommandText =
            "SELECT EXISTS(SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = $id);";
        var parameter = rowCommand.CreateParameter();
        parameter.ParameterName = "$id";
        parameter.Value = BaselineMigrationId;
        rowCommand.Parameters.Add(parameter);

        var rowResult = await rowCommand.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(rowResult) == 1;
    }

    private async Task StampBaselineMigrationAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            """
            CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                ProductVersion TEXT NOT NULL
            );
            """,
            cancellationToken);

        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            "INSERT OR IGNORE INTO __EFMigrationsHistory(MigrationId, ProductVersion) VALUES ($id, $version);";

        var id = command.CreateParameter();
        id.ParameterName = "$id";
        id.Value = BaselineMigrationId;
        command.Parameters.Add(id);

        var version = command.CreateParameter();
        version.ParameterName = "$version";
        version.Value = EfProductVersion;
        command.Parameters.Add(version);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task AddColumnIfMissingAsync(
        string table,
        string column,
        string alterSql,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info('{table}');";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var exists = false;

        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }

        await reader.DisposeAsync();

        if (!exists)
            await ExecuteAsync(alterSql, cancellationToken);
    }

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken)
    {
        DbConnection connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
