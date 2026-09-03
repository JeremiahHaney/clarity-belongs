# Roadmap

## Phase 0 — Foundation

- lock mission and brand
- secure domains and social identity
- define product families
- define shared observation architecture
- establish opportunity backlog

**Status: Complete**

## Phase 1 — Core Awareness Platform

Build the minimum shared engine needed for multiple monitors:

- account/workspace identifier boundary
- Follow + Target + SourceDefinition
- scheduled ObservationRuns
- snapshots/history
- change detection
- alert rules
- FollowChange relevance/acknowledgement
- notification records
- email alerts
- My Clarity dashboard/read models
- HTTP/TLS/DNS adapters

The V1 entity model and monitor contracts are locked in `05-CORE-OBSERVATION-DATA-MODEL.md`.

The cross-ecosystem reuse decisions are locked in `06-ECOSYSTEM-REUSE-AUDIT.md`.

Recommended implementation order:

1. solution/app shell + persistence
2. UserId / WorkspaceId ownership boundary without building a full identity platform
3. Follow / Target / SourceDefinition management
4. ObservationRun / Snapshot / Change persistence
5. port/extract AutoPilot IT `PublicEndpointGuard`
6. port/extract AutoPilot IT website/TLS HTTP check mechanics behind neutral Clarity contracts
7. general scheduler using AutoPilot IT's proven due-target / `NextCheckUtc` pattern
8. Change / FollowChange history + My Clarity read models
9. AlertRule evaluation
10. neutral notification delivery using AutoPilot IT's send-once/dedup patterns
11. DNS adapter from the existing AutoPilot IT implementation
12. domain-expiration adapter from the existing AutoPilot IT implementation

Do not block this phase on a perfect shared identity, membership, or billing platform. Keep those concerns behind simple boundaries so they can be shared later.

**Status: Architecture + ecosystem reuse review complete — implementation next**

## Phase 2 — First Product Cluster

Prioritize monitors with simple data sources and strong engine reuse:

1. Website change monitor
2. Website uptime monitor
3. SSL expiration monitor
4. Domain expiration monitor
5. DNS change monitor
6. Software/release monitor

## Phase 3 — Money & Availability

Add price/history adapters where reliable data can be obtained with acceptable operating cost:

- product prices
- product availability
- subscription price changes
- airfare
- hotels
- rental cars
- event tickets

## Phase 4 — Opportunities & Public Information

- job alerts
- grants
- bids
- public agendas
- regulatory/public-record change tracking

## Phase 5 — Identity & Reputation

- brand mentions
- typo domains
- certificate-transparency signals
- username impersonation
- reputation changes

## Release gate

A product should not ship merely because the engine can technically perform the check. It should also have:

- understandable setup
- reliable evidence/history
- sensible notification defaults
- bounded operating cost
- low expected support burden
- clear help content
