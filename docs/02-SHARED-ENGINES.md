# Shared Engines

Clarity should be built engine-first.

## Core Observation Engine

```text
source
  -> fetch / receive
  -> normalize
  -> observe
  -> store snapshot
  -> compare
  -> classify change
  -> update history
  -> evaluate notification rules
  -> notify
```

The concrete V1 data model and monitor contract are defined in `05-CORE-OBSERVATION-DATA-MODEL.md`.

Core entities:

- User
- Workspace
- WorkspaceMember
- Follow
- Target
- SourceDefinition
- ObservationRun
- Snapshot
- Change
- AlertRule
- FollowChange
- Notification

## Supporting engines

### Scheduler
Runs recurring observations with configurable cadence, retries, backoff, next-run tracking, and shared-target execution where safe.

### Comparison
Supports hashes, structured field comparisons, text differences, numeric thresholds, and before/after summaries.

### Notification
Email and in-app first, with room for push/SMS/webhook delivery when economically justified.

### Evidence / History
Preserves what was observed and when so alerts can explain why they fired.

### Source adapters
Thin adapters for HTTP pages, DNS, TLS, APIs, feeds, public data, pricing sources, software/package sources, and other monitored targets.

Initial reusable adapters:

- HTTP
- DNS
- TLS
- Feed
- API
- Manual

### Monitor definitions
Each product surface should be a thin monitor definition that validates configuration, creates the target/source definition, provides defaults, and formats current state.

### My Clarity
All monitors feed one read experience:

- Needs Attention
- Since You Were Here
- Following
- History
- Alerts
- Settings

### Search & Discovery integration
Every monitor can expose focused problem pages while sharing one underlying implementation.

## Build rule

Prefer a source adapter or comparison capability that unlocks many monitors over one isolated monitor with bespoke infrastructure.
