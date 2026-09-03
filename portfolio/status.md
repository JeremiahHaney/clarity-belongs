# Portfolio Status

## Current milestone

**Core Observation Engine + My Clarity design complete, ecosystem reuse review complete — ready for implementation.**

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
| Your Internet | First build cluster selected |
| Changes | Backlog established |
| Opportunities | Backlog established |
| Public Information | Backlog established |
| Your Identity | Backlog established |
| My Clarity | V1 data model defined |

## Core platform status

Defined:

- UserId / WorkspaceId ownership boundary
- Follow
- Target
- SourceDefinition
- ObservationRun
- Snapshot
- Change
- AlertRule
- FollowChange
- Notification
- My Clarity read models
- observation adapter contract
- comparison contract
- monitor definition contract
- scheduler/deduplication model
- V1 boundaries
- cross-ecosystem reuse rules

Reuse findings:

- AutoPilot IT already has production-oriented HTTP/website, TLS, DNS, domain, API, content, email-auth, heartbeat, security-header and endpoint-safety monitoring mechanics.
- AutoPilot IT's due-target worker and notification deduplication patterns should guide Clarity's scheduler and delivery implementations.
- Clarity should own the richer Follow -> Snapshot -> Change -> History awareness model instead of adopting APIT's operational incident model.
- Software Belongs provides useful application/product patterns but does not yet contain a mature canonical shared account/workspace/billing platform.
- Shared identity, membership and billing should remain behind boundaries and must not block Clarity's first build.

Next implementation slice:

1. app/solution shell + persistence
2. UserId / WorkspaceId boundary
3. Follow / Target / SourceDefinition
4. ObservationRun / Snapshot / Change
5. reuse/extract APIT `PublicEndpointGuard`
6. reuse/extract APIT website/TLS HTTP checking mechanics
7. general scheduler using APIT's due-target pattern
8. My Clarity history/dashboard
9. alert rules + notification delivery
10. DNS/domain adapters from APIT implementations

## Initial product cluster

1. Website Change Monitor
2. Website Uptime Monitor
3. SSL Expiration Monitor
4. Domain Expiration Monitor
5. DNS Change Monitor
6. Software / Release Monitor

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
