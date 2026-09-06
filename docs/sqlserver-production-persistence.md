# Clarity Belongs SQL Server production persistence

## Goal

Move production persistence from the current file-backed SQLite database to a dedicated SQL Server database without discarding the existing EF model, user accounts, monitoring history, billing state, notification state, or durability work.

SQLite remains acceptable for local development and targeted tests. Production should use SQL Server once migration validation is complete.

## Production target

Recommended production names:

- Database: `ClarityBelongs`
- Application principal: `ClarityBelongs_App`
- Provider setting: `Database:Provider=SqlServer`
- Connection string setting: `ConnectionStrings__ClarityBelongs`

The Clarity application principal must have access only to the Clarity Belongs database. Sharing the same SQL Server instance with AutoPilot IT and Software Belongs does not imply cross-database access.

## Authoritative data to preserve

The current Clarity model contains durable customer and operational state including:

- users
- workspaces
- memberships and Stripe identifiers
- password-reset tokens
- targets and source definitions
- follows
- observation runs
- snapshots
- detected changes
- alert rules
- follow/change links
- notifications and delivery state
- digest delivery state
- Stripe webhook replay records
- feedback submissions

No provider migration is acceptable if it requires users to recreate accounts or loses monitoring history.

## Migration strategy

1. Freeze the production SQLite file with a verified application-level SQLite backup.
2. Create the `ClarityBelongs` SQL Server database and least-privilege application principal.
3. Apply a SQL Server-compatible baseline schema representing the current EF model.
4. Copy data table-by-table while preserving primary keys, foreign keys, timestamps, password hashes, provider IDs, status values, and deduplication keys.
5. Validate row counts for every table.
6. Validate critical relationships:
   - user -> workspace
   - user/workspace -> membership
   - workspace -> follows
   - target/source -> observation runs and snapshots
   - follows -> changes/notifications
7. Validate uniqueness constraints and indexes.
8. Run the application against SQL Server in a non-public slot/environment.
9. Verify login with migrated credentials without resetting passwords.
10. Verify monitoring history, current schedules, billing state, feedback, and owner operations.
11. Restart the application and verify continuity again.
12. Switch production configuration only after all validation passes.
13. Retain the final SQLite backup as a rollback artifact for the defined rollback period.

## Rollback rule

Do not run production simultaneously against SQLite and SQL Server as two writable authorities.

If cutover fails before accepting new SQL Server writes, restore the previous SQLite configuration.

If cutover fails after accepting SQL Server writes, do not blindly switch back. First reconcile the new writes or restore SQL Server from the cutover backup point.

## Provider compatibility checks

Before cutover verify:

- string lengths are explicit where SQL Server indexes require bounded columns
- decimal precision is explicit where used
- DateTime values remain UTC
- nullable unique indexes behave as intended
- cascade-delete behavior is intentional
- generated values/identity columns preserve imported keys
- concurrency-sensitive worker claims are safe under SQL Server
- SQL Server migrations do not contain SQLite-only operations

## Production backup model

Once SQL Server is authoritative, use SQL Server-native backup/restore for production. Keep the existing SQLite backup tooling only for SQLite mode and legacy migration/rollback needs.

Production operations should define:

- automated full backup cadence
- differential/log backup policy where appropriate
- retention
- off-publish-tree backup storage
- restore verification
- alerting for failed or stale backups

## Health contract

The existing database health philosophy should remain after provider migration. `/health` should continue to report non-sensitive readiness information covering:

- reachable
- schema current
- writable
- backup freshness when a reliable SQL Server backup signal is available

Backup freshness must not be reported as healthy based on the old SQLite backup directory once SQL Server is authoritative.

## Release gate

Production SQL Server cutover is blocked until all are true:

- fresh SQL Server database creates/migrates successfully
- existing SQLite data migration succeeds on a representative copy
- row counts and relationships validate
- migrated users authenticate with existing passwords
- monitoring history and next-run state survive
- Stripe replay/subscription state survives
- notification/digest deduplication state survives
- application restart continuity passes
- backup and restore procedure is documented and tested
- IIS/server secrets are configured outside source control
- rollback procedure is documented
