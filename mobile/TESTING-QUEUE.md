# Clarity Belongs Mobile Testing Queue

Mobile implementation can proceed before device builds are available. Runtime verification is tracked separately from architecture and feature construction.

## Functional surfaces

- My Clarity dashboard
- Follow discovery and creation
- Follow detail
- Alert list/detail
- History timeline
- Before/after evidence
- Account and plan state
- Notification preferences
- Push deep links into alert/follow/history destinations
- Offline read cache for recent follows, alerts, history, and evidence

## Deferred runtime gates

- Android build/emulator/device
- iOS build/simulator/device/signing
- authentication/session persistence
- secure token storage
- push registration and delivery
- notification deep links
- offline/online reconciliation
- background refresh constraints
- accessibility and dynamic text
- store packaging/privacy declarations

The server remains authoritative for observations, comparisons, history, alerts, memberships, and billing state. Mobile caches read models and submits user actions through service/API boundaries rather than duplicating observation engines.
