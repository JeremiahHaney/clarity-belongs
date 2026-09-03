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

## 0.4.0 - 2026-09-03

- Completed Launch Phase 5: accounts, personal workspace ownership, membership limits, and the Stripe billing boundary.
- Added ASP.NET Core cookie authentication with sign-up, sign-in, sign-out, protected routes, and authenticated API access.
- Added ASP.NET Core password hashing and one-hour, single-use password-reset tokens stored only as SHA-256 hashes.
- Bound My Clarity, Follow detail, change evidence, settings, and follow-management operations to the authenticated user's personal workspace.
- Added Free, Personal, and Business membership records and plan definitions.
- Added active-follow limits and minimum check cadences: Free 5 / 6 hours, Personal 50 / 15 minutes, Business 250 / 5 minutes.
- Kept in-app alerts available on Free while enforcing paid membership at the external email-delivery boundary.
- Added a public membership/pricing page and authenticated account/membership page without inventing unapproved public dollar prices.
- Added Stripe-hosted Checkout session creation, Billing Portal session creation, customer/subscription identifiers, price synchronization, current-period state, cancellation state, and past-due handling.
- Added a signed `POST /webhooks/stripe` endpoint covering checkout completion, subscription create/update/delete, and invoice payment failure events.
- Added Stripe webhook HMAC verification with timestamp tolerance before membership state is changed.
- Added startup schema upgrades so existing development SQLite databases can receive the Phase 5 authentication, Membership, and PasswordResetToken fields/tables without being deleted.
- Added account-aware navigation and membership-aware Add Follow / Follow settings UI.
- Expanded CI from health-only verification to a real local account smoke test: create account, preserve session cookie, open the protected Account page, and create an authenticated Free-plan follow.
- Fixed the account-page Razor generated-class naming collision found by CI; the follow-up Release build and account smoke test passed.
- Added `07-ACCOUNTS-MEMBERSHIP-BILLING.md` and advanced roadmap/status documentation through Launch Phase 5.
- Kept real Stripe Checkout/webhook/portal validation and production SMTP password-reset/paid-alert delivery marked Testing Required until external provider credentials and approved Stripe prices are configured.

## 0.5.0 - 2026-09-03

- Completed Launch Phases 6 and 7: the public Clarity Belongs website plus the first search/discovery acquisition layer.
- Replaced the authenticated root route with a public homepage built around “Stop checking. Know what changed.” and moved the authenticated awareness dashboard to `/my-clarity`.
- Added public homepage sections explaining the observation loop, first five monitors, My Clarity, the DO / KNOW / HANDLE ecosystem, and Free-plan entry path.
- Expanded product discovery and product-detail pages with monitor outcomes, defaults, history/evidence framing, FAQs, and signup/Add Follow calls to action.
- Added public About, Support, draft Privacy, and draft Terms pages and expanded the public navigation/footer.
- Added a problem-first `/learn` library and 12 initial search-intent guides covering website changes, webpage notifications, uptime, SSL expiration, domain expiration, DNS changes, pricing pages, terms/privacy pages, public notices, website-change concepts, change-vs-uptime comparison, and check cadence.
- Kept initial discovery content aligned to currently implemented V1 monitors instead of advertising speculative products.
- Added internal links from Learn -> product -> signup / Add Follow and documented the future Software Belongs free-checker -> Clarity recurring-monitor referral pattern.
- Added `robots.txt` that allows public discovery while excluding authenticated/private routes from crawler guidance.
- Added an initial `sitemap.xml` for the homepage, product, pricing, About, Support, legal, and Learn surfaces.
- Added unique page titles and description metadata to public product and Learn/article pages.
- Added dedicated public responsive styles without disrupting the existing application UX.
- Expanded CI smoke coverage to request the homepage, public product route, Learn route/article, pricing, About, Support, Privacy, Terms, robots, sitemap, authenticated My Clarity, Account, and follow-creation API.
- Added `08-PUBLIC-SITE-SEARCH-DISCOVERY.md` and advanced roadmap/status documentation through Launch Phase 7.
- Kept final legal/contact details, approved paid prices, production deployment/indexing, reciprocal Software Belongs links, analytics/search-console configuration, and real-world browser/search validation marked Testing Required.

## 0.5.1 - 2026-09-03

- Fixed the Blazor programmatic heading focus outline that could draw a jagged border around page headings after navigation.
- Reused the confirmed Software Belongs fix by suppressing the outline specifically for focused `h1[tabindex="-1"]` elements, preserving normal keyboard focus behavior elsewhere.

## 0.5.2 - 2026-09-03

- Added a restrained teal/blue visual identity to the public homepage while keeping the existing calm neutral layout.
- Added lightweight inline SVG icons to the four-step observation flow without adding an external icon dependency.
- Added subtle tinted section treatment, accent bars, card hover depth, colored monitor edges, and differentiated change/recovery/reminder badges.
- Updated the Clarity ecosystem and call-to-action treatments to carry the new accent palette consistently across the page.

## 0.5.3 - 2026-09-03

- Modernized the public homepage so Clarity feels like a product company rather than an editorial/news-style site while keeping its own identity separate from Software Belongs.
- Added a Clarity brand mark built directly into the shared header with an eye/change motif and teal brand treatment.
- Rebuilt the homepage hero as a two-column product surface with a live example of watched items, change status, reminders, and history.
- Added stronger product-card iconography, monitor-specific accent colors, status pills, hover depth, and section backgrounds to increase visual hierarchy.
- Updated primary actions and header branding to use Clarity teal consistently while preserving the existing calm neutral base and responsive behavior.

## 0.5.4 - 2026-09-03

- Added a Clarity-branded social footer band across the public site.
- Linked the confirmed @claritybelongs Instagram, TikTok, and YouTube accounts with compact social cards and matching teal/dark visual treatment.
- Added the “Follow what changes” discovery message while keeping the existing legal/navigation footer below it.
- Left Reddit out until the matching Clarity account is reserved and ready to link.

## 0.6.0 - 2026-09-03

- Expanded Clarity from the initial five monitors to a complete 64-product V1 catalog across Money, Your Internet, Changes, Opportunities, Public Information, Your Identity, and Reliability.
- Kept the expansion thin by reusing the shared HTTP content, HTTP availability, TLS, DNS, RDAP domain, history, evidence, alert, membership, and notification engines.
- Added a generic DNS-over-HTTPS record observer and new DNS record adapter type for nameserver, MX, SPF/TXT, DKIM, and DMARC monitoring.
- Added V1 product surfaces for pricing, availability, travel, tickets, fees, web infrastructure, releases, jobs, grants, bids, public information, identity/reputation, and reliability use cases.
- Kept specialized source-based monitors explicit about V1 scope instead of implying unsupported private APIs, proprietary datasets, broad crawling, or structured extraction.
- Grouped the public product catalog by family and expanded the public sitemap to include every V1 product route.
- Updated README and roadmap so implementation status, product count, shared primitives, testing requirements, and later adapter refinements match the code.
- Moved Clarity's next phase from feature expansion to representative real-source testing, dogfooding, production integrations, deployment, and release validation.

## 0.6.1 - 2026-09-03

- Added the neutral `Belongs.Shared` .NET 8 project as the canonical source for cross-identity monitoring infrastructure.
- Extracted shared endpoint safety, HTTP observation, TLS observation, DNS observation, RDAP domain observation, scheduling, and notification deduplication primitives.
- Rewired Clarity's HTTP/TLS/DNS/domain adapters, scheduler cadence calculation, and notification-key generation to the shared project while preserving Clarity-specific Follow/Snapshot/Change/History behavior.
- Made AutoPilot IT consume the exact same shared project through a pinned public git submodule rather than maintaining duplicate low-level monitoring implementations.
- Kept brand-specific outcomes, awareness rules, incidents, persistence, user-facing messages, and managed-service workflows outside the shared layer.
- Verified Clarity restore/build/startup/account smoke tests remain green after the consolidation.
