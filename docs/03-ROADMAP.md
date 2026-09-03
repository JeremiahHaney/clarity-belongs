# Roadmap

## Foundation — Complete

- mission and brand locked
- domains and social identity secured
- product families defined
- shared observation architecture defined
- opportunity backlog established
- ecosystem reuse audit completed

## Launch Phase 1 — Make the engine real

- .NET 10 Blazor application shell
- EF Core SQLite persistence
- personal UserId / WorkspaceId boundary
- Follow / Target / SourceDefinition
- ObservationRun / Snapshot / Change
- scheduled observations
- shared-target recent-run reuse
- HTTP / TLS / DNS / domain adapters
- public-endpoint safety guard
- CI restore/build verification
- runtime `/health` smoke test

**Status: Implemented. CI build and runtime smoke verification added.**

## Launch Phase 2 — Core product experience

- shared Clarity navigation and layout
- My Clarity dashboard
- meaningful empty/onboarding state
- Add Follow wizard
- Follow detail page
- pause / resume
- stop following / archive
- manual check-now action
- cadence and importance settings
- alert enable/disable
- history
- acknowledgement
- before / after evidence view
- error-state experience

**Status: Implemented — Testing Required for interactive user flows.**

## Launch Phase 3 — Your Internet

Initial public monitor cluster:

1. Website Change Monitor
2. Website Uptime Monitor
3. SSL Expiration Monitor
4. Domain Expiration Monitor
5. DNS Change Monitor

Each now has:

- a dedicated product route
- product description/help
- setup flow
- monitor-specific defaults
- current Follow state
- shared history/evidence
- shared alert pipeline

Important implementation details:

- Website Change fingerprints content, final URL, and HTTP status.
- Website Uptime fingerprints stable availability state instead of response milliseconds.
- TLS fingerprints certificate identity/expiration instead of changing days-remaining values.
- Domain expiration fingerprints registry expiration data instead of changing days-remaining values.
- DNS fingerprints a normalized sorted public address set.

**Status: Implemented — Testing Required against representative real-world targets.**

## Launch Phase 4 — Notifications

- persisted in-app alerts
- persisted email delivery queue
- unique dedup keys
- meaningful-change alerts
- failure alerts
- recovery alerts
- SSL/domain expiration reminders at 30 / 14 / 7 / 1 days
- SMTP delivery provider boundary
- notification email settings
- immediate delivery mode
- optional daily digest mode: “what changed while you were not looking”
- failed/suppressed delivery tracking

Production SMTP credentials are intentionally configuration-only and are not stored in the repository.

**Status: Implemented — external SMTP delivery Testing Required after provider configuration.**

## Launch Phase 5 — Accounts + Membership

Next:

- real sign-up/sign-in
- account recovery
- personal workspace ownership from authenticated identity
- free/paid usage limits based on actual delivery cost
- subscription state
- Stripe checkout and billing portal

## Launch Phase 6 — Public Website

- public homepage
- product discovery
- pricing
- Learn/help
- mission
- privacy
- terms
- support/contact

## Launch Phase 7 — Search + Discovery

- problem-first landing pages
- how-to pages
- comparison pages
- FAQs
- Software Belongs cross-links and free-checker entry paths

## Launch Phase 8 — Dogfood

Clarity should monitor the Belongs ecosystem itself:

- softwarebelongs.com
- claritybelongs.com
- AutoPilot IT public endpoints
- domains
- certificates
- DNS
- important vendor/release pages

## Expansion — Changes

- terms monitoring
- privacy-policy monitoring
- competitor pages
- software releases
- product pages
- RSS/feed monitoring

## Expansion — Money & Availability

- product prices
- product availability
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
- the five Your Internet products pass real target tests
- interactive setup/history/acknowledgement flows pass
- SMTP delivery passes with the production provider
- accounts and subscription are complete
- claritybelongs.com public site is published
- privacy/terms/help are published
- the Belongs ecosystem is being dogfooded through Clarity
