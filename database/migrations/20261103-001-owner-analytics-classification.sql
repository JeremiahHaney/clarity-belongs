/*
 MigrationId: 20261103-001-owner-analytics-classification
 Server-verified owner event flag. Historical events remain unverified/public.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NULL
    THROW 51002, 'SchemaMigrations missing. Apply existing migrations first.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    IF COL_LENGTH('dbo.AcquisitionEvents', 'IsOwnerActivity') IS NULL
        ALTER TABLE dbo.AcquisitionEvents
            ADD IsOwnerActivity bit NOT NULL
                CONSTRAINT DF_AcquisitionEvents_IsOwnerActivity DEFAULT (0);
    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AcquisitionEvents')
          AND name = N'IX_AcquisitionEvents_OwnerVisitorId'
    )
    BEGIN
        CREATE INDEX IX_AcquisitionEvents_OwnerVisitorId
            ON dbo.AcquisitionEvents(VisitorId)
            WHERE IsOwnerActivity = 1;
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.SchemaMigrations
        WHERE MigrationId = N'20261103-001-owner-analytics-classification'
    )
        INSERT dbo.SchemaMigrations(MigrationId)
        VALUES (N'20261103-001-owner-analytics-classification');
    COMMIT TRANSACTION;
    PRINT 'PASS: 20261103-001-owner-analytics-classification';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
