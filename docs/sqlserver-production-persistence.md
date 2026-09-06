# Clarity Belongs SQL Server production persistence

## Current decision

Clarity Belongs has no production customer accounts or customer data to migrate. Production therefore starts with a fresh dedicated SQL Server database instead of carrying the current development SQLite database into production.

SQLite remains the local Development/test provider. SQL Server is the Production provider.

## Production target

- Database: `ClarityBelongs`
- Provider: `Database:Provider=SqlServer`
- Connection string: `ConnectionStrings__ClarityBelongs`
- Runtime database role: `ClarityBelongsRuntime`
- Schema/bootstrap role: `ClarityBelongsMigration`

The Clarity database remains isolated from AutoPilot IT and Software Belongs even when they share one SQL Server instance.

## Runtime behavior

`appsettings.json` selects SQL Server. `appsettings.Development.json` selects SQLite.

Production startup fails fast when `ConnectionStrings__ClarityBelongs` is missing. The SQL Server provider initializes the current EF model into an empty database and then validates connectivity and writability. Existing SQLite migrations and legacy-schema adoption run only in SQLite mode.

SQLite `--backup-database` and `--restore-database` commands are rejected while SQL Server is selected. Production backup and restore belong to SQL Server operations.

## First production initialization

Because the runtime role is intentionally not granted DDL permissions, initialize the empty database with a deployment/migration identity before switching the application to its normal runtime identity.

1. Run `deployment/sqlserver/clarity-database-boundary.sql` with a SQL Server administrative identity.
2. Create/map the dedicated deployment identity and add it to `ClarityBelongsMigration`.
3. Configure `ConnectionStrings__ClarityBelongs` for that deployment identity.
4. Start Clarity once against the empty `ClarityBelongs` database so EF creates the current model schema and startup validation passes.
5. Stop/recycle Clarity.
6. Map the normal IIS/application identity and add it only to `ClarityBelongsRuntime`.
7. Change `ConnectionStrings__ClarityBelongs` to the runtime identity/credentials.
8. Start Clarity normally and verify `/health` reports `SqlServer`, reachable, schema current, and writable.

No SQLite copy/import step is required.

## Model compatibility

The EF model now explicitly bounds SQL Server index-key strings including:

- user email
- target canonical key
- password reset token hash
- monitor type
- adapter type
- notification deduplication/channel/status
- Stripe event/customer/subscription/price identifiers

`SqlServerModelCompatibilityTests` verifies indexed string properties cannot silently regress to unbounded SQL Server key columns.

## Schema evolution boundary

The current Production SQL Server schema is a pre-launch EF model baseline created with `EnsureCreated` because there is no customer data or migration history to preserve.

Before the first schema change after real production users/data exist:

1. generate a SQL Server-native EF migration baseline/history,
2. stop using `EnsureCreated` as the schema-evolution mechanism,
3. require reviewed migrations for every subsequent production schema change.

Do not run the existing SQLite migrations against SQL Server.

## Backups

Use SQL Server-native backup/restore for Production. At minimum define:

- automated full backups,
- retention,
- backup storage outside IIS/publish directories,
- alerting for failed/stale backups,
- periodic restore verification.

SQLite backup tooling remains for local Development/test databases only.

## Release verification

Before enabling public signup:

- dedicated `ClarityBelongs` database exists,
- application runtime identity has no sibling-database access,
- runtime identity is not `db_owner`,
- schema bootstrap completed with the migration identity,
- `/health` reports SQL Server reachable/current/writable,
- signup creates a user/workspace/membership,
- logout/login works after application restart,
- a follow can be created and remains after restart,
- SQL Server backup succeeds,
- a restore test has been performed,
- secrets remain outside source control.
