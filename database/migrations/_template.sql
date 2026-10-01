/*
    <Product> production migration
    MigrationId: YYYYMMDD-NNN-short-description

    Run against the production database before publishing the code that depends on it.
    Keep the script idempotent whenever practical.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF EXISTS
(
    SELECT 1
    FROM dbo.SchemaMigrations
    WHERE MigrationId = N'YYYYMMDD-NNN-short-description'
)
BEGIN
    PRINT 'Already applied: YYYYMMDD-NNN-short-description';
    RETURN;
END;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /*
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
GO
