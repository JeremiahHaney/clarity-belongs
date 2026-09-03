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

Core entities should eventually cover:

- Source
- Observation
- Snapshot
- Change
- Subscription / Follow
- Rule / Threshold
- Notification
- History event

## Supporting engines

### Scheduler
Runs recurring observations with configurable cadence, retries, backoff, and next-run tracking.

### Comparison
Supports hashes, structured field comparisons, text differences, numeric thresholds, and before/after summaries.

### Notification
Email first, with room for push/SMS/webhook delivery when economically justified.

### Evidence / History
Preserves what was observed and when so alerts can explain why they fired.

### Source adapters
Thin adapters for HTTP pages, DNS, TLS, APIs, feeds, public data, pricing sources, software/package sources, and other monitored targets.

### Search & Discovery integration
Every monitor can expose focused problem pages while sharing one underlying implementation.

## Build rule

Prefer a source adapter or comparison capability that unlocks many monitors over one isolated monitor with bespoke infrastructure.
