using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Data;

public static class AcquisitionAnalyticsSchema
{
    public static async Task EnsureAsync(
        ClarityDbContext db,
        CancellationToken cancellationToken = default)
    {
        var sql = db.Database.IsSqlServer()
            ? SqlServerSql
            : SqliteSql;

        await db.Database.ExecuteSqlRawAsync(
            sql,
            cancellationToken);
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

    private const string SqlServerSql =
        """
        IF OBJECT_ID(N'[AcquisitionEvents]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AcquisitionEvents] (
                [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AcquisitionEvents] PRIMARY KEY,
                [VisitorId] NVARCHAR(64) NOT NULL,
                [UserId] BIGINT NULL,
                [WorkspaceId] BIGINT NULL,
                [EventType] NVARCHAR(64) NOT NULL,
                [Path] NVARCHAR(500) NULL,
                [ProductSlug] NVARCHAR(100) NULL,
                [FollowId] BIGINT NULL,
                [Source] NVARCHAR(100) NULL,
                [Medium] NVARCHAR(100) NULL,
                [Campaign] NVARCHAR(150) NULL,
                [OccurredAtUtc] DATETIME2 NOT NULL
            );
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = N'IX_AcquisitionEvents_VisitorId_OccurredAtUtc'
                AND object_id = OBJECT_ID(N'[AcquisitionEvents]'))
        BEGIN
            CREATE INDEX [IX_AcquisitionEvents_VisitorId_OccurredAtUtc]
                ON [AcquisitionEvents] ([VisitorId], [OccurredAtUtc]);
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = N'IX_AcquisitionEvents_UserId_OccurredAtUtc'
                AND object_id = OBJECT_ID(N'[AcquisitionEvents]'))
        BEGIN
            CREATE INDEX [IX_AcquisitionEvents_UserId_OccurredAtUtc]
                ON [AcquisitionEvents] ([UserId], [OccurredAtUtc]);
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = N'IX_AcquisitionEvents_EventType_OccurredAtUtc'
                AND object_id = OBJECT_ID(N'[AcquisitionEvents]'))
        BEGIN
            CREATE INDEX [IX_AcquisitionEvents_EventType_OccurredAtUtc]
                ON [AcquisitionEvents] ([EventType], [OccurredAtUtc]);
        END;
        """;
}
