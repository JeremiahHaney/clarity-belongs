# Clarity Belongs database operations

Clarity V1 intentionally uses SQLite for a single application instance. Customer accounts, workspaces, follows, observation history, changes, notifications, feedback, and scheduled `NextCheckAtUtc` values all live in this database, so the database file is production state and must not live in a replaceable publish directory.

## Durable database location

Database storage is resolved in this order:

1. `CLARITY_DB_PATH`
2. `DatabaseStorage:Path`
3. Development only: `<content-root>/.data/clarity.db`
4. Production default on Windows: `%ProgramData%\ClarityBelongs\Data\clarity.db`
5. Production default on Linux: `/var/lib/ClarityBelongs/Data/clarity.db`

Backup storage is resolved in this order:

1. `CLARITY_BACKUP_DIR`
2. `DatabaseStorage:BackupDirectory`
3. `<database-directory>/backups`

Production deployments should explicitly set `CLARITY_DB_PATH` and `CLARITY_BACKUP_DIR` even though safe OS-level defaults exist. Do not place either path under the IIS site root, Web Deploy destination, publish output, container image layer, or another directory replaced during deployment.

The application creates the configured data and backup directories when its process identity has permission to do so. It does not attempt to elevate privileges or change ownership.

### IIS permissions

Provision the production directories once before first start and grant the application pool identity Modify permission. Example, run from an elevated PowerShell session and substitute the actual pool name:

```powershell
$DataRoot = Join-Path $env:ProgramData "ClarityBelongs\Data"
$BackupRoot = Join-Path $DataRoot "backups"
$AppPoolIdentity = "IIS AppPool\ClarityBelongs"

New-Item -ItemType Directory -Force -Path $DataRoot | Out-Null
New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

$Acl = Get-Acl $DataRoot
$Rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $AppPoolIdentity,
    "Modify",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow")
$Acl.SetAccessRule($Rule)
Set-Acl -Path $DataRoot -AclObject $Acl
```

Set `CLARITY_DB_PATH` to `%ProgramData%\ClarityBelongs\Data\clarity.db` and `CLARITY_BACKUP_DIR` to `%ProgramData%\ClarityBelongs\Data\backups` in the deployment environment unless a different durable volume is intentionally used.

## Schema evolution

`20260905183011_InitialClarityBaseline` is the EF Core migration baseline.

Fresh databases are created by `Database.MigrateAsync()`. Existing databases created by the pre-migration V1 code are adopted without rebuilding their tables:

1. `DatabaseSchemaService` recognizes a legacy database by the existing `Users` table and absence of the Clarity baseline migration history.
2. It applies only the legacy compatibility mutations that existed before migrations: the password/email-verification columns and membership, reset-token, and feedback tables/indexes.
3. It records the baseline in `__EFMigrationsHistory`.
4. EF Core applies any migrations newer than the baseline.
5. The application validates that no migrations remain pending and that the database is reachable and writable before serving requests.

Once the baseline exists, the handwritten schema upgrader no longer mutates schema. New schema work should be created as EF migrations and committed with an updated model snapshot.

Use the design-time factory when creating migrations:

```powershell
dotnet ef migrations add <MigrationName> --project src/ClarityBelongs.Web --startup-project src/ClarityBelongs.Web
```

Do not delete or recreate `__EFMigrationsHistory` on a production database.

## Startup failure behavior

Database initialization happens before the web app begins serving requests. If directory access, legacy adoption, migration, connectivity, or write validation fails, startup throws and logs a critical error. Clarity does not continue against a partially upgraded or unverified schema.

## Backup

Clarity uses the SQLite online backup API, not a naive copy of a live `.db` file. The backup API creates a transactionally consistent snapshot while the application may still be running.

From the deployed application directory, with the same database environment configuration used by the site:

```powershell
dotnet ClarityBelongs.Web.dll --backup-database
```

The command validates startup/schema state, writes a timestamped database into the configured backup directory, runs `PRAGMA integrity_check` on the backup, and reports only the backup file name and size. Database filesystem paths are not returned by the public health endpoint.

Recommended V1 schedule: at least one daily backup plus a backup immediately before deployment or schema migration. Copy backup files to a second durable location using normal server backup tooling; a backup on the same disk is not disaster recovery.

## Restore

Restore is an offline operator action. Do not restore while the IIS application is serving requests.

1. Stop the Clarity IIS application pool/site.
2. Confirm the desired backup file is present in the configured backup directory.
3. From the deployed application directory, run:

```powershell
dotnet ClarityBelongs.Web.dll --restore-database <backup-file-name>.db
```

4. The restore command first creates a safety backup of the current database when one exists.
5. It validates the selected backup with `PRAGMA integrity_check`, restores through the SQLite backup API, validates the restored database again, then runs the normal migration/startup validation path.
6. Start the IIS application pool/site.
7. Confirm `/health` is healthy and manually verify an existing user, follow, history item, and scheduled next-check value.

The restore command accepts a file name from the configured backup directory only; it does not accept an arbitrary filesystem path.

## Health

`/health` reports non-sensitive database state:

- database reachable
- schema current
- database writable
- last known backup UTC timestamp
- backup age in hours

It does not expose the connection string or database/backup paths.

A missing backup does not by itself make the app unhealthy; it is an operational warning condition. Migration/connectivity/write failure prevents normal startup.

## Deployment continuity

The database is deliberately outside the publish/Web Deploy tree. Web application binaries may therefore be replaced without replacing customer state. `NextCheckAtUtc` and all other scheduled/follow state are persisted in SQLite and reloaded after restart.

Before deployment:

1. Run the supported backup command.
2. Verify the backup completed.
3. Deploy application files.
4. Startup applies pending EF migrations before serving traffic.
5. Verify `/health` and a representative existing account/follow.

## Current scaling boundary

SQLite V1 assumes:

- one Clarity application instance owns the database
- the database resides on a local durable filesystem
- no horizontal multi-node web/worker scaling against the same SQLite file
- normal SQLite locking/concurrency characteristics are acceptable for current V1 traffic

Do not put the SQLite database on a general-purpose network share to simulate multi-node storage.

When traffic or availability requirements justify multiple application instances, move persistence to SQL Server or PostgreSQL. Keep EF Core entity/migration discipline, add the new provider, create a provider-aware migration/data-transfer plan, validate row counts and critical state, perform a controlled cutover, and only then enable horizontal scale. That provider migration is intentionally outside the current V1 hardening scope.
