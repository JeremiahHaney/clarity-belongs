/*
    Adds minimal first-party acquisition analytics to an existing Clarity Belongs
    SQL Server database.

    Run with an identity that belongs to ClarityBelongsMigration.
    The web/runtime identity should not receive DDL permissions.
*/

USE [ClarityBelongs];
GO

IF OBJECT_ID(N'[dbo].[AcquisitionEvents]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AcquisitionEvents]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT [PK_AcquisitionEvents] PRIMARY KEY,
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
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AcquisitionEvents_VisitorId_OccurredAtUtc'
        AND [object_id] = OBJECT_ID(N'[dbo].[AcquisitionEvents]')
)
BEGIN
    CREATE INDEX [IX_AcquisitionEvents_VisitorId_OccurredAtUtc]
        ON [dbo].[AcquisitionEvents] ([VisitorId], [OccurredAtUtc]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AcquisitionEvents_UserId_OccurredAtUtc'
        AND [object_id] = OBJECT_ID(N'[dbo].[AcquisitionEvents]')
)
BEGIN
    CREATE INDEX [IX_AcquisitionEvents_UserId_OccurredAtUtc]
        ON [dbo].[AcquisitionEvents] ([UserId], [OccurredAtUtc]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AcquisitionEvents_EventType_OccurredAtUtc'
        AND [object_id] = OBJECT_ID(N'[dbo].[AcquisitionEvents]')
)
BEGIN
    CREATE INDEX [IX_AcquisitionEvents_EventType_OccurredAtUtc]
        ON [dbo].[AcquisitionEvents] ([EventType], [OccurredAtUtc]);
END;
GO
