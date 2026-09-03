# Portfolio Status

## Current milestone

**Core Awareness Platform V1 implemented — Testing Required.**

## Identity

- Name: Clarity Belongs
- Role: KNOW
- Primary domain: claritybelongs.com
- Defensive domain: claritybelongs.net
- Social identity: @claritybelongs

## Product families

| Family | Status |
|---|---|
| Money | Backlog established |
| Your Internet | Core adapters implemented; product surfaces next |
| Changes | Core HTTP/change engine implemented |
| Opportunities | Backlog established |
| Public Information | Backlog established |
| Your Identity | Backlog established |
| My Clarity | V1 dashboard implemented — Testing Required |

## Core platform status

Implemented:

- .NET 10 Blazor Web App shell
- SQLite / EF Core persistence
- UserId / WorkspaceId ownership boundary with automatic personal workspace bootstrap
- Follow
- Target
- SourceDefinition
- ObservationRun
- Snapshot
- Change
- AlertRule
- FollowChange
- Notification with persisted dedup keys
- My Clarity Needs Attention / Recent Changes / Following dashboard
- HTTP observation adapter
- TLS observation adapter
- DNS observation adapter
- domain expiration / RDAP observation adapter
- APIT-derived `PublicEndpointGuard` behavior
- APIT-derived website HTTP/TLS mechanics
- generalized due-follow BackgroundService scheduler
- shared-target recent-run deduplication window
- generic fingerprint change detection
- change fan-out to all follows sharing a target
- in-app alert creation and send-once/dedup behavior
- API surface for creating follows, manual runs, and acknowledgements

## Reuse boundary

- AutoPilot IT remains the operational monitoring / incident / recovery consumer.
- Clarity owns Follow -> Observation -> Snapshot -> Change -> History -> Alert.
- Low-level public endpoint, HTTP/TLS, DNS, domain and scheduler mechanics are intentionally aligned with the proven AutoPilot IT implementations.
- Software Belongs remains the DO surface and can consume shared platform mechanics later without blocking Clarity.

## Testing Required

The implementation is committed, but the current environment could not reach GitHub from the local build container, so a real `dotnet restore/build/run` has not yet been completed here.

Validate next:

1. `dotnet restore ClarityBelongs.slnx`
2. `dotnet build ClarityBelongs.slnx`
3. run `src/ClarityBelongs.Web`
4. create one HTTP follow and verify initial snapshot
5. change the target or wait for a real change and verify Change + FollowChange + Notification
6. test TLS, DNS and domain expiration follows
7. verify scheduler cadence and duplicate-target reuse
8. verify My Clarity dashboard and acknowledgement flow

## Initial product cluster

1. Website Change Monitor — engine available; product setup surface next
2. Website Uptime Monitor — engine available; product setup surface next
3. SSL Expiration Monitor — engine available; product setup surface next
4. Domain Expiration Monitor — engine available; product setup surface next
5. DNS Change Monitor — engine available; product setup surface next
6. Software / Release Monitor — adapter/product specialization next

## Lifecycle

Use these states for products:

- Captured
- Validate
- Ready
- Building
- Testing Required
- Released
- Parked
- Rejected
