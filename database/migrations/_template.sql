/*
    Clarity Belongs production migration
    MigrationId: YYYYMMDD-NNN-short-description

    Run this script while connected to the correct production database.
    Keep the script idempotent whenever practical.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaMigrations
    (
        MigrationId NVARCHAR(200) NOT NULL
            CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
        AppliedUtc DATETIME2 NOT NULL
            CONSTRAINT DF_SchemaMigrations_AppliedUtc DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.SchemaMigrations
    WHERE MigrationId = N'YYYYMMDD-NNN-short-description'
)
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        /*
            Validate an application-specific baseline table before changing schema.

            Schema changes go here.

            Examples:
            - Guard new tables with OBJECT_ID(...)
            - Guard new columns with COL_LENGTH(...)
            - Guard new indexes with sys.indexes
        */

        INSERT INTO dbo.SchemaMigrations(MigrationId)
        VALUES (N'YYYYMMDD-NNN-short-description');

        COMMIT TRANSACTION;
        PRINT 'PASS: YYYYMMDD-NNN-short-description';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END
ELSE
BEGIN
    PRINT 'Already applied: YYYYMMDD-NNN-short-description';
END;
GO
