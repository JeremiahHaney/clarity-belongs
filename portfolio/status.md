# Portfolio Status

## Current milestone

**Launch Phases 1–7 implemented. Core engine, local accounts/membership, and public-site/search routes are covered by CI. Real monitor targets, production SMTP/Stripe, legal finalization, and live deployment remain Testing Required.**

## Identity

- Name: Clarity Belongs
- Role: KNOW
- Primary domain: claritybelongs.com
- Defensive domain: claritybelongs.net
- Social identity: @claritybelongs

## Product families

| Family | Status |
|---|---|
| Money | Backlog established |
| Your Internet | First five product surfaces implemented — Testing Required |
| Changes | Shared HTTP/change/history engine implemented; initial public search surfaces live in code |
| Opportunities | Backlog established |
| Public Information | Initial public-notice discovery entry exists; product family backlog established |
| Your Identity | Backlog established |
| My Clarity | Dashboard + follow/history/evidence + account/membership UX implemented — Testing Required |

## Launch Phase 1 — Engine

Implemented and CI verified:

- .NET 10 Blazor Web App
- EF Core SQLite persistence
- UserId / WorkspaceId ownership boundary
- Follow / Target / SourceDefinition
- ObservationRun / Snapshot / Change
- AlertRule / FollowChange / Notification
- HTTP / TLS / DNS / RDAP domain observations
- APIT-derived public-endpoint protection
- due-Follow scheduler
- retry/error behavior
- shared-target reuse window
- stable monitor-specific fingerprints
- `/health`

## Launch Phase 2 — Core experience

Implemented:

- My Clarity dashboard at `/my-clarity`
- Add Follow
- Follow detail
- Check now
- Pause / Resume / archive
- cadence / importance / alert settings
- history
- acknowledgement
- before/after evidence
- responsive app UX

## Launch Phase 3 — Your Internet

Implemented product surfaces:

1. Website Change Monitor
2. Website Uptime Monitor
3. SSL Expiration Monitor
4. Domain Expiration Monitor
5. DNS Change Monitor

## Launch Phase 4 — Notifications

Implemented:

- in-app alerts
- email queue
- unique dedup keys
- meaningful-change / failure / recovery alerts
- SSL/domain expiration thresholds
- SMTP boundary
- Immediate / DailyDigest modes
- Sent / Failed / Suppressed tracking
- paid email entitlement

## Launch Phase 5 — Accounts + Membership

Implemented and locally CI verified:

- signup / signin / signout
- password hashing and reset tokens
- personal workspace ownership
- Free / Personal / Business memberships
- plan follow and cadence limits
- account/membership UX
- Stripe Checkout / Billing Portal / signed webhook boundary
- additive SQLite schema upgrades

## Launch Phase 6 — Public website

Implemented:

- public homepage at `/`
- homepage explanation of the KNOW identity
- product catalog and expanded product-detail pages
- public pricing path
- About / ecosystem page
- Support
- draft Privacy
- draft Terms
- public navigation/footer
- signup entry points
- separate authenticated `/my-clarity` route
- public responsive styling

## Launch Phase 7 — Search + Discovery

Implemented:

- public `/learn` discovery library
- 12 initial problem-first guide routes
- how-to, FAQ, comparison, and explanation content structures
- product <-> Learn internal linking
- initial search intents aligned only to working V1 monitors
- `robots.txt`
- `sitemap.xml`
- page titles / descriptions on discovery surfaces
- Software Belongs cross-link strategy documented
- CI expanded to request homepage, products, Learn, legal/help, robots, sitemap, authenticated dashboard, and account routes

## Security / dependency hygiene

- public HTTP/TLS monitoring blocks private/local endpoints
- password-reset raw tokens are not stored
- Stripe webhook signatures are verified
- protected data is workspace-scoped
- Stripe and SMTP credentials are not committed
- private application routes are excluded from public crawler guidance
- SQLite native dependency is pinned forward from vulnerable transitive 2.1.11

## Testing Required before public release

1. Exercise all five Your Internet products against representative real targets.
2. Validate pause/resume/archive/history/evidence with real accounts.
3. Configure production SMTP and test password reset plus paid alerts/digest.
4. Configure approved Stripe products/prices and verify Checkout, webhooks, cancellation/past-due, and Billing Portal.
5. Review Phase 6 on desktop/mobile browsers.
6. Replace draft privacy/terms language with final business/legal/contact details.
7. Deploy to claritybelongs.com and verify canonical production behavior.
8. Configure analytics/search indexing tools and confirm sitemap discovery.
9. Add reciprocal Software Belongs discovery links.
10. Dogfood the full Belongs ecosystem through Clarity.

## Next build milestone

Launch Phase 8 — Dogfood the system, then use the real usage findings to decide whether to expand Changes first or add another Clarity family.

## Lifecycle

- Captured
- Validate
- Ready
- Building
- Testing Required
- Released
- Parked
- Rejected
