using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Data;

public static class AcquisitionAnalyticsSchema
{
    public static async Task EnsureAsync(
        ClarityDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (db.Database.IsSqlServer())
        {
            await VerifySqlServerSchemaAsync(
                db,
                cancellationToken);
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            SqliteSql,
            cancellationToken);
    }

    private static async Task VerifySqlServerSchemaAsync(
        ClarityDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = db.Database
                .GetDbConnection()
                .CreateCommand();
            command.CommandText =
                "SELECT CASE WHEN OBJECT_ID(N'[AcquisitionEvents]', N'U') IS NULL THEN 0 ELSE 1 END;";

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (Convert.ToInt32(result) != 1)
            {
                throw new InvalidOperationException(
                    "The SQL Server AcquisitionEvents table is missing. Run deployment/sqlserver/add-acquisition-analytics.sql with the ClarityBelongsMigration identity before starting the runtime.");
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private const string SqliteSql =
        """
        CREATE TABLE IF NOT EXISTS AcquisitionEvents (
            Id INTEGER NOT NULL CONSTRAINT PK_AcquisitionEvents PRIMARY KEY AUTOINCREMENT,
            VisitorId TEXT NOT NULL,
            UserId INTEGER NULL,
            WorkspaceId INTEGER NULL,
            EventType TEXT NOT NULL,
            Path TEXT NULL,
            ProductSlug TEXT NULL,
            FollowId INTEGER NULL,
            Source TEXT NULL,
            Medium TEXT NULL,
            Campaign TEXT NULL,
            OccurredAtUtc TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_AcquisitionEvents_VisitorId_OccurredAtUtc
            ON AcquisitionEvents (VisitorId, OccurredAtUtc);
        CREATE INDEX IF NOT EXISTS IX_AcquisitionEvents_UserId_OccurredAtUtc
            ON AcquisitionEvents (UserId, OccurredAtUtc);
        CREATE INDEX IF NOT EXISTS IX_AcquisitionEvents_EventType_OccurredAtUtc
            ON AcquisitionEvents (EventType, OccurredAtUtc);
        """;
}
