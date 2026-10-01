/*
    Clarity Belongs manual SQL baseline.
    Run while connected to the production Clarity Belongs SQL Server database.
    The script does not switch databases.

    Safe to run more than once.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    THROW 51000, 'Users is missing. Production is not at the expected Clarity Belongs baseline.', 1;

IF OBJECT_ID(N'dbo.Workspaces', N'U') IS NULL
    THROW 51001, 'Workspaces is missing. Production is not at the expected Clarity Belongs baseline.', 1;

IF OBJECT_ID(N'dbo.Memberships', N'U') IS NULL
    THROW 51002, 'Memberships is missing. Production is not at the expected Clarity Belongs baseline.', 1;

IF OBJECT_ID(N'dbo.StripeWebhookEvents', N'U') IS NULL
    THROW 51003, 'StripeWebhookEvents is missing. Production is not at the expected Clarity Belongs baseline.', 1;

IF OBJECT_ID(N'dbo.DigestDeliveryStates', N'U') IS NULL
    THROW 51004, 'DigestDeliveryStates is missing. Production is not at the expected Clarity Belongs baseline.', 1;

IF OBJECT_ID(N'dbo.AcquisitionEvents', N'U') IS NULL
    THROW 51005, 'AcquisitionEvents is missing. Run the existing acquisition analytics SQL script first.', 1;
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
    WHERE MigrationId = N'20261001-000-manual-sql-baseline'
)
BEGIN
    INSERT INTO dbo.SchemaMigrations(MigrationId)
    VALUES (N'20261001-000-manual-sql-baseline');
END;
GO

PRINT 'Clarity Belongs manual SQL baseline: PASS';
GO
