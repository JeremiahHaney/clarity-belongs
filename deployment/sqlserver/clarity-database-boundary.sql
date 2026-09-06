/*
    Clarity Belongs SQL Server database boundary.

    Run with an administrative deployment identity.
    This script intentionally does not create server logins, passwords, or application users.
    Map the IIS/application identity to the runtime role separately on the target server.
*/

IF DB_ID(N'ClarityBelongs') IS NULL
BEGIN
    CREATE DATABASE [ClarityBelongs];
END;
GO

USE [ClarityBelongs];
GO

IF DATABASE_PRINCIPAL_ID(N'ClarityBelongsRuntime') IS NULL
BEGIN
    CREATE ROLE [ClarityBelongsRuntime];
END;
GO

IF DATABASE_PRINCIPAL_ID(N'ClarityBelongsMigration') IS NULL
BEGIN
    CREATE ROLE [ClarityBelongsMigration];
END;
GO

GRANT CONNECT TO [ClarityBelongsRuntime];
GRANT SELECT TO [ClarityBelongsRuntime];
GRANT INSERT TO [ClarityBelongsRuntime];
GRANT UPDATE TO [ClarityBelongsRuntime];
GRANT DELETE TO [ClarityBelongsRuntime];
GO

GRANT CONNECT TO [ClarityBelongsMigration];
ALTER ROLE [db_datareader] ADD MEMBER [ClarityBelongsMigration];
ALTER ROLE [db_datawriter] ADD MEMBER [ClarityBelongsMigration];
ALTER ROLE [db_ddladmin] ADD MEMBER [ClarityBelongsMigration];
GO

/*
    Example only after the target server identity is known:

    CREATE USER [DOMAIN\ClarityBelongsAppPool] FOR LOGIN [DOMAIN\ClarityBelongsAppPool];
    ALTER ROLE [ClarityBelongsRuntime] ADD MEMBER [DOMAIN\ClarityBelongsAppPool];

    Use a separate deployment identity for schema migrations and add it to
    ClarityBelongsMigration. Do not grant the runtime identity db_owner.
*/
