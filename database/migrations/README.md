# Production database migrations

Clarity Belongs production SQL Server schema changes are deployed as ordered SQL files.

## Workflow

1. Pull the repository on the VPS.
2. Back up the production database.
3. Run any new files in this folder in filename order.
4. Confirm the script prints PASS and records its MigrationId in dbo.SchemaMigrations.
5. Publish the matching Clarity build.

The production application does not create or migrate SQL Server schema. It verifies that the required manual SQL migration is present and refuses to start against an older schema.

SQLite remains a development/local path and may continue using EF migrations there.

## Rules for new migrations

- File name: YYYYMMDD-NNN-short-description.sql
- Make scripts idempotent whenever practical.
- Guard CREATE TABLE, ALTER TABLE, and CREATE INDEX operations.
- Use SET XACT_ABORT ON.
- End by inserting the same filename ID into dbo.SchemaMigrations.
- Update ProductionSchema.RequiredMigrationId when a new migration becomes required by the application.
