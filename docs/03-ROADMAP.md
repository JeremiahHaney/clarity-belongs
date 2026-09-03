# Roadmap

## Foundation — Complete

- mission and brand locked
- domains and social identity secured
- product families defined
- shared observation architecture defined
- opportunity backlog established
- ecosystem reuse audit completed

## Launch Phase 1 — Engine

Implemented and CI verified: .NET 10 Blazor, EF Core SQLite, User/Workspace boundary, Follow/Target/SourceDefinition, ObservationRun/Snapshot/Change, scheduler, HTTP/TLS/DNS/domain adapters, public-endpoint guard, shared-target reuse, `/health`.

## Launch Phase 2 — Core product experience

Implemented: My Clarity, onboarding, Add Follow, Follow detail, pause/resume/archive, check now, cadence/importance/alerts, history, acknowledgement, before/after evidence, error experience.

**Status: Implemented — representative interactive flows remain Testing Required.**

## Launch Phase 3 — Your Internet

1. Website Change Monitor
2. Website Uptime Monitor
3. SSL Expiration Monitor
4. Domain Expiration Monitor
5. DNS Change Monitor

**Status: Implemented — real target testing remains required.**

## Launch Phase 4 — Notifications

Implemented: in-app alerts, email queue, deduplication, failure/recovery, SSL/domain expiration reminders, SMTP boundary, Immediate and DailyDigest modes, delivery state tracking.

**Status: Implemented — production SMTP delivery remains Testing Required.**

## Launch Phase 5 — Accounts + Membership

Implemented: signup/signin/signout, password reset, personal workspace ownership, Free/Personal/Business membership state, usage/cadence limits, paid email entitlement, Stripe Checkout/Portal/webhook boundary, additive SQLite upgrade, CI account smoke test.

**Status: Implemented — external Stripe and SMTP flows remain Testing Required.**

See `07-ACCOUNTS-MEMBERSHIP-BILLING.md`.

## Launch Phase 6 — Public Website

Implemented:

- public homepage at `/`
- authenticated dashboard moved to `/my-clarity`
- public product discovery and expanded product-detail pages
- pricing entry path
- public About / mission page
- Learn/help entry path
- Support page
- draft Privacy page
- draft Terms page
- signup entry paths from homepage/product pages
- public navigation/footer
- responsive public-site styling

**Status: Implemented — browser/mobile presentation, final legal copy, production contact details, approved prices, and live domain deployment remain Testing Required.**

## Launch Phase 7 — Search + Discovery

Implemented initial acquisition surface:

- problem-first Learn library
- 12 initial search-intent guide pages
- how-to pages
- explanation pages
- comparison page
- FAQ structures
- product-to-guide and guide-to-product internal linking
- `robots.txt` separating public discovery from authenticated application routes
- `sitemap.xml` covering current public pages
- unique page titles and description metadata for product/article pages
- Software Belongs ecosystem/cross-link direction documented

Initial intents include website-change monitoring, webpage-change alerts, uptime, SSL expiration, domain expiration, DNS changes, pricing-page changes, terms/privacy changes, public notices, change-vs-uptime comparison, and monitor cadence.

**Status: Implemented — production indexing, analytics/Search Console, keyword performance, Software Belongs reciprocal links, and content expansion remain Testing Required.**

See `08-PUBLIC-SITE-SEARCH-DISCOVERY.md`.

## Launch Phase 8 — Dogfood

Next:

- softwarebelongs.com
- claritybelongs.com
- AutoPilot IT public endpoints
- domains
- certificates
- DNS
- important vendor/release pages

## Expansion Wave 1 — Public-source monitors

Implemented as thin product surfaces over the existing HTTP content-change and uptime engines:

1. Restock & Availability Monitor
2. Recall Alert Monitor
3. Local Government Project Monitor
4. School & Community Notice Monitor
5. Fee Change Monitor
6. Service Outage Monitor
7. Price & Sale History Monitor
8. Cancellation & Change Monitor
9. Deadline Monitor
10. Consumer Notice Monitor

These monitors intentionally watch user-selected public sources and preserve evidence/history. Specialized structured extraction, source discovery, retailer integrations, private-account access, and authoritative external datasets remain future work rather than being implied by V1.

**Status: Product definitions implemented — representative real-source testing remains required.**

## Expansion — Changes

- terms monitoring
- privacy-policy monitoring
- competitor pages
- software releases
- product pages
- RSS/feed monitoring

## Expansion — Money & Availability

- structured product price extraction
- structured product availability extraction
- subscription price changes
- airfare
- hotels
- rental cars
- event tickets

## Expansion — Opportunities & Public Information

- jobs
- grants
- bids
- public agendas
- regulatory filings
- policy changes
- public records

## Expansion — Identity & Reputation

- brand mentions
- typo domains
- certificate-transparency signals
- username impersonation
- reputation changes

## Scale / Harden

Only as real usage requires it:

- PostgreSQL / SQL Server evaluation
- distributed workers / leases
- durable queueing
- retention policies
- object storage for larger evidence
- quotas and rate limits
- telemetry
- backup/recovery
- abuse protection

## Public V1 release gate

Clarity V1 is release-ready when:

- engine build/startup verification is green
- public-site/search route smoke verification is green
- five Your Internet products pass real target tests
- interactive setup/history/acknowledgement flows pass
- SMTP delivery passes with the production provider
- Stripe Checkout/webhook/portal flows pass in test mode
- approved product prices are configured
- claritybelongs.com is deployed
- final privacy/terms/support contact details are published
- Software Belongs reciprocal discovery links are live
- the Belongs ecosystem is being dogfooded through Clarity
