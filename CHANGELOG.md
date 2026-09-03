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

## 0.2.0 - 2026-09-03

- Added the first runnable Clarity Belongs .NET 10 Blazor Web App and solution shell.
- Added EF Core SQLite persistence and automatic bootstrap of a personal User / Workspace ownership boundary.
- Implemented Follow, Target, SourceDefinition, ObservationRun, Snapshot, Change, AlertRule, FollowChange, and Notification persistence.
- Ported the AutoPilot IT public-endpoint safety approach into Clarity's reusable `PublicEndpointGuard`.
- Implemented HTTP observation with redirect handling, public-endpoint validation, response timing, status capture, content snapshots, normalized JSON and fingerprints.
- Implemented TLS certificate observation using the proven AutoPilot IT socket/TLS mechanics.
- Implemented DNS observation based on the AutoPilot IT DNS checker behavior.
- Implemented RDAP domain-expiration observation based on the AutoPilot IT domain checker behavior.
- Generalized the AutoPilot IT due-target worker pattern into a due-Follow background scheduler.
- Added a short recent-run reuse window so multiple follows of one Target + SourceDefinition can avoid duplicate checks.
- Implemented snapshot fingerprint comparison, Change creation, and fan-out through FollowChange records.
- Implemented default AnyMeaningfulChange rules and persisted in-app notification records with send-once/dedup keys.
- Added My Clarity dashboard read models and a Blazor dashboard for Needs Attention, Recent Changes, and Following.
- Added APIs to create follows, manually run a follow, and acknowledge a change.
- Marked the core platform Testing Required because a local build could not be run from the current environment after GitHub became unreachable from the build container.

## 0.3.0 - 2026-09-03

- Completed launch phases 1–4 for the first Clarity Belongs V1 product experience.
- Added GitHub Actions restore/build verification and a Release-mode runtime smoke test against `/health`.
- Fixed the solution-path and Blazor routing-import issues exposed by CI and reached a green build/startup verification run.
- Added shared application navigation, polished My Clarity, useful empty states, responsive layout, and an error page.
- Added a guided Add Follow experience and full Follow detail workflow with check-now, pause/resume, archive, cadence, importance, alert settings, history, and acknowledgement.
- Added an evidence view that preserves and displays normalized before/after observations for each meaningful change.
- Added the initial Your Internet product catalog and dedicated setup surfaces for Website Change, Website Uptime, SSL Expiration, Domain Expiration, and DNS Change monitoring.
- Separated HTTP content-change fingerprints from uptime fingerprints so response-time variation does not create false changes.
- Stabilized TLS and domain-expiration fingerprints around certificate/registry facts rather than continuously changing days-remaining values.
- Updated the scheduler so failed follows remain eligible for retry instead of becoming permanently stuck in Error.
- Added operational failure and recovery alerts in addition to meaningful target-change alerts.
- Added persisted email delivery records alongside in-app notifications with per-channel deduplication.
- Added SMTP delivery configuration, user notification-email settings, Sent/Failed/Suppressed delivery tracking, and optional Immediate or DailyDigest delivery modes.
- Added SSL/domain expiration threshold reminders at 30, 14, 7, and 1 day with send-once deduplication.
- Added the daily digest framing: what changed while you were not looking.
- Added CI runtime verification that starts the Release application and checks the live health endpoint.
- Pinned `SQLitePCLRaw.lib.e_sqlite3` forward to 2.1.12 to avoid the vulnerable transitive 2.1.11 native SQLite package.
- Advanced launch phases 1–4 to implemented while keeping real monitor targets, interactive flows, and external SMTP delivery marked Testing Required.
