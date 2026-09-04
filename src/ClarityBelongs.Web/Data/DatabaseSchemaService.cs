using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace ClarityBelongs.Web.Data;

public sealed class DatabaseSchemaService(ClarityDbContext db)
{
    public async Task UpgradeAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);

        try
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
        finally
        {
            await db.Database.CloseConnectionAsync();
        }

        await EnsureMembershipsAsync(cancellationToken);
    }

    private async Task EnsureMembershipsAsync(CancellationToken cancellationToken)
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
