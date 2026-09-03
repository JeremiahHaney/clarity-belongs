# Ecosystem Reuse Audit

Date: 2026-09-03

## Purpose

Before Clarity Belongs begins implementation, review Software Belongs and AutoPilot IT for reusable code and architecture so Clarity does not independently rebuild platform capabilities that already exist.

## Executive conclusion

Clarity should proceed, but not as a completely independent technical island.

The three brands should remain distinct products:

- Software Belongs = DO
- Clarity Belongs = KNOW
- AutoPilot IT = HANDLE

However, several underlying mechanics are shared enough that Clarity should either reuse existing implementations or establish neutral contracts that AutoPilot IT and Software Belongs can also consume later.

The highest-value reuse is currently in AutoPilot IT monitoring. Software Belongs has strong reusable application patterns and many product engines, but its account/workspace/shared-platform layer is not yet mature enough to treat as the canonical platform implementation.

## What already exists

### AutoPilot IT monitoring

AutoPilot IT already contains a substantial monitoring implementation:

- `AutoPilotIT-Monitoring` project
- website monitor checker
- DNS checker
- domain-expiration checker
- email-auth checker
- heartbeat/backup checker
- API endpoint checker
- content keyword checker
- security-header checker
- public-endpoint guard
- monitoring target/result/incident/notification models
- background monitoring worker
- check executor
- notification service
- monitoring database schema
- public status-page support
- webhook event publishing

This is not a skeleton. It is working product-domain code and should be treated as prior art for Clarity.

### Software Belongs

Software Belongs contains many reusable application patterns and domain engines, including document, PDF, image, media, OCR, social-planner, and other tool families.

It also contains the beginning of a workspace abstraction in Social Planner, but the current `Workspace` and `WorkspaceService` are only empty shells. They are not yet a reusable account/workspace platform implementation.

No mature shared membership/billing/account platform implementation was identified during this audit.

## Reuse decisions

### 1. Monitoring check mechanics — REUSE / EXTRACT

Do not independently reimplement basic website, TLS, DNS, domain-expiration, API, or endpoint-safety checks in Clarity.

The AutoPilot IT implementations should be the starting point.

Preferred direction:

```text
Belongs.Observation.Checks
    HTTP
    TLS
    DNS
    Domain
    API
    Content
    SecurityHeaders
    EndpointSafety

        |                 |
        v                 v
Clarity Belongs      AutoPilot IT
```

For the first Clarity build, copying code into a neutral project is acceptable if cross-repository packaging would slow development. The important rule is that Clarity should not invent a second incompatible implementation.

### 2. Observation/history model — CLARITY SHOULD LEAD

AutoPilot IT monitoring is operational-status oriented. Its primary model is:

```text
Target -> CheckResult -> Status -> Incident -> Notification
```

Clarity needs a richer awareness model:

```text
Follow
  -> Target
  -> ObservationRun
  -> Snapshot
  -> Change
  -> History
  -> Rule
  -> Notification
```

Therefore, do not force Clarity into the existing APIT monitoring database model.

Clarity's observation/history model should remain the canonical model for general-purpose change awareness.

Later, APIT can consume the shared observation/check layer while preserving APIT-specific incidents, remediation, status pages, and operational state.

### 3. Scheduler — REUSE PATTERN, REBUILD AS GENERAL ENGINE

AutoPilot IT currently runs a background worker once per minute, selects active targets whose `NextCheckUtc` is due, limits each batch, executes checks, and advances the next run.

That is a good proven pattern.

Clarity should use the same operational approach initially, but put scheduling behind a neutral service/contract so it can grow to support:

- configurable cadence
- retries/backoff
- jitter
- target deduplication
- cost-aware frequency
- paused follows
- source-specific minimum cadence
- future distributed workers

Do not make APIT's `MonitoringWorker` a direct Clarity dependency.

### 4. Notification delivery — EXTRACT CONCEPT, NOT APIT BRAND LOGIC

AutoPilot IT already has email delivery, notification deduplication, expiry thresholds, failure/recovery alerts, and notification persistence.

Reuse the mechanics and patterns:

- email sender abstraction
- deduplication keys
- persisted notification record
- send-once semantics
- safe HTML encoding

Do not reuse APIT-specific message copy, URLs, operational statuses, or recipient lookup directly.

Clarity should own rule evaluation and message meaning. A future neutral `Belongs.Notifications` layer can own delivery.

### 5. Endpoint safety / SSRF protection — REUSE

AutoPilot IT already includes a `PublicEndpointGuard` for public HTTP checks.

This is infrastructure/security logic, not product-specific logic, and should be reused or extracted before Clarity allows arbitrary user-provided URLs.

### 6. Accounts / users / workspaces — DEFINE NEUTRAL BOUNDARY NOW

Neither Software Belongs nor the inspected Clarity code currently provides a mature shared identity/workspace implementation suitable to become the ecosystem standard.

Clarity should therefore avoid deeply coupling domain entities to a Clarity-specific user model.

Use simple identifiers and interfaces so a shared account/membership platform can be introduced later without rewriting the observation engine.

Recommended boundary:

```text
UserId
WorkspaceId
EntitlementProvider
WorkspaceProvider
```

The domain engine should not care how login, Stripe, membership, or organization administration is implemented.

### 7. Billing / membership — DO NOT BUILD INSIDE CLARITY ENGINE

No canonical shared implementation was identified in this audit.

Treat billing and entitlements as an external platform concern.

Clarity monitors should expose usage units such as:

- active follows
- check frequency
- retained history
- notification volume
- premium source adapters

A membership system can map plans to those limits later.

## Brand-specific responsibilities

### Software Belongs owns

- utility/product execution
- local-first tools
- document/media/data engines
- creator/business/developer tools
- Learn and problem-first discovery

### Clarity Belongs owns

- follows
- observations
- snapshots
- comparisons
- change history
- personal awareness rules
- My Clarity
- consumer/business awareness UX

### AutoPilot IT owns

- operational infrastructure monitoring UX
- incidents
- recovery state
- public status pages
- endpoint health
- remediation
- managed-service workflows
- customer operational responsibility

## Shared technical candidates

Create neutral shared components only when at least two brands materially need them.

Highest-confidence candidates:

1. `Belongs.EndpointSafety`
2. `Belongs.HttpObservation`
3. `Belongs.DnsObservation`
4. `Belongs.TlsObservation`
5. `Belongs.DomainObservation`
6. `Belongs.Scheduling`
7. `Belongs.Notifications`

Potential later candidates:

8. `Belongs.Identity`
9. `Belongs.Workspaces`
10. `Belongs.Membership`
11. `Belongs.Billing`
12. `Belongs.History`

Do not create all of these immediately. Extract only as active implementation proves the boundary.

## Clarity implementation adjustment

The Clarity build order should now be:

```text
1. Clarity app shell + persistence
2. UserId / WorkspaceId boundary only
3. Follow / Target / SourceDefinition
4. ObservationRun / Snapshot / Change
5. Port/extract APIT PublicEndpointGuard
6. Port/extract APIT website/TLS HTTP check mechanics
7. General Clarity scheduler using APIT's proven due-target pattern
8. My Clarity read models
9. Alert rules
10. Neutral notification delivery using APIT's send/dedup patterns
11. DNS adapter from APIT implementation
12. Domain-expiration adapter from APIT implementation
```

## What not to do

Do not:

- make Clarity reference `AutoPilotIT.Web`
- reuse APIT-branded domain models as Clarity's core model
- copy APIT notification copy or URLs
- build a second independent URL safety implementation
- build a second unrelated HTTP/TLS/DNS checker
- prematurely create a giant shared-platform repository before boundaries are proven
- block Clarity waiting for a perfect shared account/billing system

## Result

The architecture is safe to continue.

Clarity remains the owner of the general awareness/history model, while AutoPilot IT provides proven reusable monitoring mechanics. Software Belongs remains the best source for app structure and product-engine patterns, but not yet for a canonical shared identity/workspace/billing layer.
