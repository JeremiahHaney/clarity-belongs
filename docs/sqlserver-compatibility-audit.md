# Clarity Belongs SQL Server compatibility audit

This audit records the concrete work required before `ClarityBelongs.Web` may use SQL Server as its production persistence provider.

## Current status

SQL Server is **not yet a selectable runtime provider**. This is intentional. The current SQLite path is authoritative until every blocker below is resolved and the cutover validation passes.

## Provider-specific blockers

### EF migration history

Current migrations were generated for SQLite and contain SQLite-specific store types and annotations such as:

- `INTEGER`
- `TEXT`
- `Sqlite:Autoincrement`

Do not run these migrations against SQL Server. Create a separate SQL Server-native baseline that represents the current `ClarityDbContext` model.

### Legacy schema adoption

`DatabaseSchemaService` contains SQLite-only schema inspection and upgrade SQL including:

- `sqlite_master`
- `PRAGMA table_info`
- `CREATE TABLE IF NOT EXISTS`
- `INSERT OR IGNORE`
- SQLite `ALTER TABLE ... ADD COLUMN` forms

The legacy adoption bridge must execute only for SQLite. A fresh SQL Server database must use its own native baseline/migration history.

### Startup writability probe

`DatabaseStartupService.VerifyWritableAsync` currently uses a SQLite temporary-table command. SQL Server needs a provider-specific writability probe that does not leave durable customer data behind.

### Backup and restore

`DatabasePathProvider` and `SqliteBackupService` are SQLite-specific. Once SQL Server is authoritative:

- SQL Server-native backup/restore owns production recovery.
- SQLite backup/restore remains available only for SQLite mode and migration rollback artifacts.
- `/health` must not infer SQL Server backup freshness from the SQLite backup directory.

### Command-line backup/restore

The `--backup-database` and `--restore-database` application arguments currently operate on SQLite files. They must be rejected or clearly scoped to SQLite mode after provider selection is introduced.

### Design-time factory

`ClarityDesignTimeDbContextFactory` is SQLite-specific. SQL Server migrations require a separate design-time context/factory or migration assembly so provider histories cannot be mixed accidentally.

## SQL Server index compatibility

Several current indexes include string columns. SQL Server cannot use unbounded `nvarchar(max)` columns as normal index keys.

Before generating the SQL Server baseline, define explicit SQL-safe lengths or an alternate key strategy for:

- `AppUser.Email` — unique index; bounded email length is appropriate.
- `Target.CanonicalKey` — unique index; requires special care because canonical URLs/keys can be long. Prefer a bounded canonical key plus collision-safe hash/index strategy rather than silent truncation.
- `Follow.MonitorType` — part of a composite index; bounded enum-like value.
- `SourceDefinition.AdapterType` — part of a composite index; bounded enum-like value.
- `Notification.DedupKey` — unique index; define a deterministic bounded key or hash.
- `Notification.Channel` — part of delivery-state index; bounded enum-like value.
- `Notification.Status` — part of delivery-state index; bounded enum-like value.
- `StripeWebhookEvent.EventId` — unique index; bounded provider identifier.
- `Membership.StripeCustomerId` and `Membership.StripeSubscriptionId` — indexed provider identifiers.

The SQL Server baseline must fail review if any indexed string remains an unbounded key column.

## Identity/key preservation

The SQLite-to-SQL Server copy must preserve existing numeric primary keys because relationships are already materialized across workspaces, follows, observations, snapshots, changes, notifications, billing state, and feedback.

The import process therefore needs explicit SQL Server identity-insert handling where identity columns are used. New SQL Server identity seeds must be advanced beyond the highest imported key before accepting writes.

## Date/time behavior

All persisted application timestamps are treated as UTC. SQL Server conversion must preserve the exact UTC values and avoid local-time conversion during import.

The SQL Server model should use a consistent date/time representation and tests must verify ordering of:

- observation schedules
- detected changes
- notification retries
- digest delivery state
- Stripe event ordering
- password reset expiry

## Worker concurrency

The current observation worker selects due follow IDs with a read query and then executes them. That is safe under the current single-instance SQLite deployment assumption, but it is not an atomic distributed claim.

Before running multiple Clarity web/worker instances against SQL Server, add an atomic claim/lease mechanism for due follows. Otherwise two instances can select and process the same follow concurrently.

Notification delivery already has durable claim/retry state; preserve and validate its atomic behavior under SQL Server transaction semantics.

## Cutover validation artifacts

Use `ClarityBelongs.DatabaseTool` against the verified SQLite cutover backup to produce the baseline inventory manifest.

The manifest records:

- integrity result
- foreign-key violations
- applied/pending migrations
- source file hash
- row count for every durable table

After the target is populated, produce the candidate manifest and use the reconciliation mode to fail the cutover when any durable table count differs.

Row-count equality is necessary but not sufficient. The final cutover still requires relationship checks, migrated-user login validation, monitoring-history validation, billing/replay validation, and restart continuity.

## Runtime provider release gate

Do not add or enable a production `UseSqlServer` switch until all are complete:

1. SQL Server-native migration context/history exists.
2. Indexed string strategy is explicit and tested.
3. Provider-specific schema/startup/backup code is separated cleanly.
4. Fresh SQL Server baseline creation succeeds.
5. SQLite inventory validation passes.
6. Data copy preserves IDs and relationships.
7. Reconciliation manifest passes.
8. Existing user passwords authenticate unchanged.
9. Monitoring schedules/history survive.
10. Billing and webhook replay state survive.
11. Restart continuity passes.
12. SQL Server backup/restore is tested.
13. Rollback procedure is rehearsed.
