# Changelog

## 0.6.8 - 2026-09-05

- Hardened SMTP configuration validation, sender/from/reply-to behavior, subject/body sanitization, and launch gating so email delivery is never advertised or attempted publicly without a complete secure configuration.
- Added durable notification delivery attempts with atomic claims, stale-claim recovery, bounded exponential backoff, terminal dead-letter handling, sanitized failure details, and existing deduplication preserved across change, failure, recovery, and expiration notifications.
- Replaced the in-memory daily-digest sent flag with durable per-user/per-day digest execution state so digest delivery is restart-safe and duplicate-resistant.
- Hardened Stripe webhook processing with signature validation, durable event replay protection, event ordering guards, safer configured-price plan mapping, cancellation/past-due downgrade behavior, and generic customer-facing provider failures with actionable internal logging.
- Enforced effective plan entitlements so only active/trialing paid memberships receive paid follow limits, cadence, visible history, email, and digest capabilities; inactive paid memberships fall back to Free behavior.
- Made the public launch explicitly Free-only by default: paid checkout and billing-management affordances are hidden, paid plan placeholders are removed, approved display prices are required before paid plans can be exposed, and email delivery remains disabled behind an explicit public gate.
- Made password-reset UX truthful when email is disabled by withholding unusable reset tokens and directing customers to support instead of claiming a reset email was sent.
- Added automated billing/email coverage for configuration validation, success/failure/retry/dead-letter delivery, duplicate prevention, restart-safe digest state, reset email generation, Stripe signature/replay/order handling, inactive-plan entitlements, pricing gating, and sanitization.
- Added the EF Core `20260905185427_HardenBillingAndEmailDelivery` migration and compatibility handling for existing pre-migration databases that already contain the hardened delivery/billing schema.

## 0.6.7 - 2026-09-05

- Added deterministic automated behavioral coverage for the Phase 1 HTTP, TLS, RDAP/domain, DNS address, and DNS record monitoring engines, including all 14 approved Phase 1 service identities.
- Added end-to-end observation lifecycle tests proving queued/running/succeeded/failed transitions, snapshot persistence, no-change deduplication, single-change creation, FollowChange linkage, alert-rule matching, notification queueing, failure state, and recovery state.
- Added file-backed SQLite restart coverage proving follows, observation history, and scheduling survive service-host recreation, plus workspace-isolation and pause/resume/archive scheduler tests.
- Added configurable durable SQLite storage outside the publish tree, with production OS-level defaults and explicit `CLARITY_DB_PATH` / `CLARITY_BACKUP_DIR` overrides.
- Added the EF Core `20260905183011_InitialClarityBaseline` migration and a legacy adoption bridge so existing V1 databases are upgraded and stamped without rebuilding customer tables or discarding data; future schema evolution now belongs to EF migrations.
- Added safe SQLite online backup and offline restore commands using the SQLite backup API, integrity validation, timestamped/versioned backups, and a pre-restore safety backup.
- Added fail-fast database startup validation for migration state, connectivity, and writability plus non-sensitive database/backup status in `/health`.
- Added integration coverage for fresh migration, pre-migration adoption, older supported schema upgrade, schema idempotence, backup/restore, restart continuity, persisted `NextCheckAtUtc`, and migration/startup failure behavior.
- Added the production database operations runbook covering IIS permissions, backup/restore, deployment continuity, single-instance SQLite limits, and the eventual SQL Server/PostgreSQL scale-up boundary.
- Fixed HTTP status monitoring so received 4xx/5xx responses are preserved as observable Down states rather than being discarded as transport failures, allowing Broken Link and HTTP Status monitors to retain meaningful status history and recovery transitions.
- Added injectable TLS and DNS test seams while preserving the production public-IP validation and pinned outbound transport security boundary.
- Hardened CI to run the monitoring behavioral suite alongside the existing runtime, public-site, account, security, database backup, and persistence smoke checks.

## 0.6.6 - 2026-09-04

- Added the public `/tools` catalog for ten free local Clarity PowerShell utilities covering notes, research folders, Markdown, transcripts, reading files, and reference organization.
- Added direct `.ps1` downloads sourced from the canonical `products/scripts` implementations so the public download matches the version in source control and QA.
- Added Free Tools to authenticated navigation, public navigation, and the footer without mixing these local utilities into Clarity's recurring monitoring catalog.
- Preserved the identity boundary: monitoring remains the core Clarity service while local document/information utilities are optional free tools.

## 0.6.5 - 2026-09-04

- Added `portfolio/desktop.md` to define Clarity Belongs' focused desktop strategy for private, local, large, or awkward-to-upload documents.
- Defined a 10-product desktop slate centered on Clarity Reader, Document Compare, Local OCR, redaction, privacy checking, local search, organization, batch processing, transcript cleanup, and the combined Clarity Desktop shell.
- Locked the identity boundary: Clarity desktop is for understanding and working with private/local documents, while generic utilities remain in Software Belongs and device diagnosis/support remains in AutoPilot IT.
- Defined a shared-engine approach so Clarity can reuse common Belongs desktop, OCR, indexing, document, and filesystem capabilities without duplicating code.

## 0.6.4 - 2026-09-03

- Simplified Clarity around one clear customer promise: tell Clarity what matters and it keeps an eye on it for you.
- Rewrote the homepage to lead with things customers care about instead of individual monitor implementations.
- Reframed the public catalog as monitoring capabilities underneath one Clarity service rather than dozens of separate apps.
- Renamed public navigation from Products to What It Watches and updated the brand/footer language around the simplified service model.
- Added `docs/clarity-product-direction.md` to lock the distinction between Clarity observation/history and Software Belongs consumer-action tools.

## 0.6.3 - 2026-09-03

- Reworked the authenticated Clarity navigation around My Clarity, Watch Something, Products, Learn, and Account so the signed-in experience behaves like an application rather than a marketing site.
- Rebuilt `/add` as a guided Watch Something experience with clearer hierarchy, grouped monitor selection, stronger target input, plan context, and a more product-like two-column layout.
- Added a Website Essentials watch pack that accepts one website/domain and creates Website Uptime, Website Change, SSL Expiration, Domain Expiration, and DNS Change follows together.
- Kept watch packs on the existing Follow and monitoring engines so the new UX does not introduce a separate execution model.
- Added plan-capacity validation before creating a pack so Free-plan users do not begin a pack they cannot finish.
- Added post-creation success guidance and related-monitor recommendations so one configured watch naturally leads to useful next watches.
- Added target query prefill support for recommendation links and expanded responsive styling for the new watch flow.

## 0.6.2 - 2026-09-03

- Added a development-only localhost `/dev/login` route for quickly exploring authenticated Clarity flows without storing temporary credentials in the repository.
- The route creates or reuses a local `Clarity Explorer` Free-plan account, signs it in for seven days, and redirects directly to `/my-clarity`.
- Restricted the helper to the Development environment and loopback requests so it is unavailable in Production deployments.

## 0.1.0 - 2026-09-03

- Established Clarity Belongs as the ecosystem's KNOW identity.
- Locked the mission: clarity belongs to everyone; help people see what changed, understand what matters, and make better decisions.
- Defined the initial product families: Money, Your Internet, Changes, Opportunities, Public Information, Your Identity, and My Clarity.
- Defined the shared observation loop: source -> observe -> store -> compare -> history -> alert -> user.
- Added the initial opportunity backlog and roadmap for a reusable monitoring/awareness platform.
- Recorded the primary and defensive domains and the shared @claritybelongs social identity.