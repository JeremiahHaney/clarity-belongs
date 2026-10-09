SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaMigrations
    (
        MigrationId nvarchar(200) NOT NULL CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
        AppliedUtc datetime2 NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedUtc DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.SchemaMigrations
    WHERE MigrationId = '20261009-001-acquisition-session-journeys'
)
BEGIN
    IF COL_LENGTH('dbo.AcquisitionEvents', 'SessionId') IS NULL
    BEGIN
        ALTER TABLE dbo.AcquisitionEvents
            ADD SessionId nvarchar(64) NULL;
    END;

    IF COL_LENGTH('dbo.AcquisitionEvents', 'DurationMs') IS NULL
    BEGIN
        ALTER TABLE dbo.AcquisitionEvents
            ADD DurationMs int NULL;
    END;

    IF COL_LENGTH('dbo.AcquisitionEvents', 'EventDetail') IS NULL
    BEGIN
        ALTER TABLE dbo.AcquisitionEvents
            ADD EventDetail nvarchar(120) NULL;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AcquisitionEvents')
          AND name = 'IX_AcquisitionEvents_SessionId'
    )
    BEGIN
        CREATE INDEX IX_AcquisitionEvents_SessionId
            ON dbo.AcquisitionEvents(SessionId)
            WHERE SessionId IS NOT NULL;
    END;

    INSERT INTO dbo.SchemaMigrations(MigrationId)
    VALUES ('20261009-001-acquisition-session-journeys');
END;

COMMIT TRANSACTION;

PRINT 'PASS: 20261009-001-acquisition-session-journeys';
