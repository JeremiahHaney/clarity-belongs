# Changelog

## 0.1.0 - 2026-09-03

- Established Clarity Belongs as the ecosystem's KNOW identity.
- Locked the mission: clarity belongs to everyone; help people see what changed, understand what matters, and make better decisions.
- Defined the initial product families: Money, Your Internet, Changes, Opportunities, Public Information, Your Identity, and My Clarity.
- Defined the shared observation loop: source -> observe -> store -> compare -> history -> alert -> user.
- Added the initial opportunity backlog and roadmap for a reusable monitoring/awareness platform.
- Recorded the primary and defensive domains and the shared @claritybelongs social identity.

## 0.1.1 - 2026-09-03

- Defined the implementation-ready Core Observation Engine and My Clarity data model.
- Added User, Workspace, Follow, Target, SourceDefinition, ObservationRun, Snapshot, Change, AlertRule, FollowChange, and Notification entities.
- Defined My Clarity dashboard/read models for Needs Attention, Since You Were Here, Following, History, Alerts, and Settings.
- Defined source adapter, comparison, and monitor-definition contracts so new monitors can remain thin product surfaces.
- Added scheduler target deduplication guidance, evidence/history rules, notification separation, and privacy/cost boundaries.
- Locked HTTP, TLS, and DNS as the first reusable adapters and documented the Phase 1 implementation order.
- Advanced the portfolio milestone from engine design to implementation-ready.

## 0.1.2 - 2026-09-03

- Completed a high-level reuse audit across Software Belongs, Clarity Belongs, and AutoPilot IT.
- Confirmed AutoPilot IT already contains reusable website/HTTP, TLS, DNS, domain, API, content, security-header, heartbeat, email-auth, and endpoint-safety monitoring mechanics.
- Chose to reuse or extract those low-level monitoring mechanics rather than create incompatible Clarity implementations.
- Kept Clarity's richer Follow -> Observation -> Snapshot -> Change -> History model as the canonical general-awareness domain model instead of adopting AutoPilot IT's operational incident model.
- Adopted AutoPilot IT's due-target worker pattern as the starting point for a neutral Clarity scheduler and its notification deduplication/send-once patterns as guidance for delivery.
- Confirmed Software Belongs has useful application/product patterns but does not yet provide a mature canonical shared identity/workspace/billing platform.
- Locked identity, membership, billing, and workspace administration behind simple boundaries so they do not block Clarity implementation.
- Added `06-ECOSYSTEM-REUSE-AUDIT.md` and aligned the roadmap and portfolio status with the reuse decisions.
