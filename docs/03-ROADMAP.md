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

## Launch Phase 3 — Initial Your Internet cluster

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
- authenticated dashboard at `/my-clarity`
- public product discovery and product-detail pages
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
- `sitemap.xml` covering public product and content pages
- unique page titles and description metadata for product/article pages
- Software Belongs ecosystem/cross-link direction documented

**Status: Implemented — production indexing, analytics/Search Console, keyword performance, Software Belongs reciprocal links, and content expansion remain Testing Required.**

See `08-PUBLIC-SITE-SEARCH-DISCOVERY.md`.

## Launch Phase 8 — Complete V1 catalog

Implemented 64 selectable V1 monitor entry points across:

- Money
- Your Internet
- Changes
- Opportunities
- Public Information
- Your Identity
- Reliability

The complete initial opportunity backlog is represented as working self-service follows. Products reuse a small set of observation primitives:

- HTTP content history
- HTTP availability/final-destination state
- TLS certificate state
- DNS address state
- generic DNS record state through DNS-over-HTTPS
- RDAP domain state

Specialized products that depend on ratings, reputation, performance reports, prices, listings, public notices, or provider status use a user-selected public source in V1. The UI and product copy explicitly say this rather than implying access to private accounts, proprietary datasets, or unsupported extraction.

DNS-record V1 support adds normalized public NS, MX, TXT/SPF, DKIM, and DMARC observations.

**Status: Implemented — representative real-source testing across product families remains required.**

## Launch Phase 9 — Dogfood and release testing

Next:

- softwarebelongs.com
- claritybelongs.com
- AutoPilot IT public endpoints
- domains
- certificates
- DNS and email records
- important vendor/release pages
- public pricing and policy pages
- representative public-information sources
- representative opportunity/listing sources

## Later refinement — only after V1 usage proves value

Do not create separate product codebases. Improve shared adapters when real usage justifies it:

- structured price and availability extraction
- retailer/travel integrations where legally and operationally sensible
- isolated keyword/selector extraction
- dedicated app-store and package-registry adapters
- dedicated reputation/security data integrations
- browser-based PageSpeed/Core Web Vitals observation
- broad source discovery and mention search
- automatic typo-domain generation
- inbound heartbeat collection for jobs/backups/mail rather than public health-URL observation
- multi-source outage aggregation
- RSS/feed-specific normalization

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
- representative HTTP, TLS, DNS, DNS-record, and domain monitors pass real target tests
- representative products from each V1 family pass Add Follow -> history -> change -> alert testing
- interactive setup/history/acknowledgement flows pass
- SMTP delivery passes with the production provider
- Stripe Checkout/webhook/portal flows pass in test mode
- approved product prices are configured
- claritybelongs.com is deployed
- final privacy/terms/support contact details are published
- Software Belongs reciprocal discovery links are live
- the Belongs ecosystem is being dogfooded through Clarity
