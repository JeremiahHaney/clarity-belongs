# Clarity Belongs SQL Server compatibility status

Clarity is pre-launch with no production customer data. The production persistence decision is therefore a fresh SQL Server database rather than a SQLite-to-SQL Server data migration.

## Resolved in 0.6.11

- Production can select the SQL Server EF provider.
- Development defaults to SQLite.
- Production requires `ConnectionStrings__ClarityBelongs` and fails fast when it is missing.
- SQLite migrations and legacy schema adoption execute only in SQLite mode.
- SQL Server uses a provider-specific temporary-table writability probe.
- SQLite backup/restore commands are rejected when SQL Server is selected.
- `/health` identifies the active provider without exposing connection details.
- Indexed string properties are explicitly bounded for SQL Server compatibility.
- Automated model coverage fails when an indexed string property becomes unbounded.
- Runtime and schema/bootstrap database roles are separated in the SQL Server provisioning script.

## Pre-launch schema baseline

The first empty SQL Server database uses EF `EnsureCreated` to materialize the current model. This is intentional only because no production customer data or SQL Server migration history exists yet.

The existing migration files remain SQLite-specific and must never be executed against SQL Server.

Before the first schema change after real production data exists, replace the pre-launch `EnsureCreated` baseline with normal SQL Server-native EF migration history and reviewed migrations.

## Remaining operational checks

These require the real SQL Server/IIS environment rather than repository code:

- create/map the deployment and runtime identities,
- initialize the empty schema with the deployment identity,
- confirm the runtime identity cannot access AutoPilot IT or Software Belongs databases,
- verify `/health` against the real SQL Server,
- perform signup/login/restart persistence smoke testing,
- enable and verify SQL Server-native backups,
- perform a restore test.

## Scale boundary

The current observation worker is appropriate for the present single application instance. It does not atomically claim due follows across multiple web/worker instances.

Do not horizontally scale Clarity workers until an atomic SQL Server claim/lease mechanism is added. This is not required for the current launch architecture.
