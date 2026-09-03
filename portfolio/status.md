# Portfolio Status

## Current milestone

**Launch Phases 1–5 implemented. Core build/startup and local account/workspace/membership flows are CI verified. Real product targets, SMTP, and Stripe external flows remain Testing Required.**

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
| Changes | Shared HTTP/change/history engine implemented |
| Opportunities | Backlog established |
| Public Information | Backlog established |
| Your Identity | Backlog established |
| My Clarity | Dashboard + follow/history/evidence + account/membership UX implemented — Testing Required |

## Launch Phase 1 — Engine

Implemented:

- .NET 10 Blazor Web App
- EF Core SQLite persistence
- UserId / WorkspaceId ownership boundary
- Follow / Target / SourceDefinition
- ObservationRun / Snapshot / Change
- AlertRule / FollowChange / Notification
- HTTP observation
- TLS observation
- DNS observation
- RDAP domain-expiration observation
- APIT-derived public-endpoint protection
- generalized due-Follow scheduler
- retries for Error follows
- shared-target short reuse window
- stable monitor-specific fingerprints
- `/health` endpoint
- GitHub Actions restore/build/runtime smoke verification

## Launch Phase 2 — Core experience

Implemented:

- shared site layout/navigation
- My Clarity dashboard
- empty/onboarding state
- Add Follow wizard
- Follow detail
- Check now
- Pause / Resume
- Stop following / archive
- cadence settings
- importance settings
- alert enable/disable
- history
- change acknowledgement
- before/after evidence page
- latest alert history
- settings page
- error page
- responsive styling

## Launch Phase 3 — Your Internet

Implemented product surfaces:

1. Website Change Monitor
2. Website Uptime Monitor
3. SSL Expiration Monitor
4. Domain Expiration Monitor
5. DNS Change Monitor

Each uses the same Follow -> Observation -> Snapshot -> Change -> History -> Alert pipeline while applying monitor-specific observation semantics.

## Launch Phase 4 — Notifications

Implemented:

- in-app alert records
- queued email notifications
- unique dedup keys
- meaningful-change alerts
- monitor failure alerts
- recovery alerts
- SSL/domain expiration reminders at 30 / 14 / 7 / 1 days
- SMTP sender boundary
- account notification email settings
- immediate email mode
- optional daily digest mode
- Sent / Failed / Suppressed tracking
- paid-plan entitlement check before email delivery

Production SMTP remains configuration-only. No credentials are committed.

## Launch Phase 5 — Accounts + Membership

Implemented:

- sign up
- sign in
- sign out
- secure ASP.NET Core password hashes
- forgot-password request flow
- one-hour, single-use password-reset tokens
- cookie sessions
- one personal My Clarity workspace per new account
- workspace ownership checks throughout user-facing follow/evidence APIs and pages
- Free / Personal / Business membership state
- Free: 5 active follows, 6-hour minimum cadence
- Personal: 50 active follows, 15-minute minimum cadence
- Business: 250 active follows, 5-minute minimum cadence
- plan-aware Add Follow and Follow settings
- account/membership page
- public pricing surface
- Stripe Checkout session creation
- Stripe Billing Portal session creation
- verified Stripe webhook endpoint
- Stripe customer/subscription/price/state persistence
- cancellation and past-due state handling
- additive SQLite schema upgrade for existing development databases
- CI account smoke test

The CI smoke path now creates a fresh account, retains the auth cookie, opens the protected account page, and creates an authenticated Free-plan follow.

## Security / dependency hygiene

- public HTTP/TLS monitoring blocks private/local endpoint targets
- password-reset raw tokens are not stored
- Stripe webhook signatures are verified before membership updates
- auth form posts require same-origin Origin/Referer validation
- protected data is scoped by authenticated workspace
- Stripe and SMTP credentials are not committed
- the SQLite native dependency is explicitly pinned forward from the vulnerable transitive 2.1.11 release

## Reuse boundary

- AutoPilot IT remains the operational monitoring / incident / recovery consumer.
- Clarity owns Follow -> Observation -> Snapshot -> Change -> History -> Alert.
- Low-level public endpoint, HTTP/TLS, DNS, domain and scheduler mechanics are intentionally aligned with the proven AutoPilot IT implementations.
- Software Belongs remains the DO surface and can consume shared platform mechanics later without blocking Clarity.

## Testing Required

Still validate before public release:

1. Create and operate each of the five Your Internet follows through the browser UI against representative targets.
2. Confirm pause/resume, archive, acknowledgement, history, and before/after flows with a real account.
3. Configure production SMTP and verify password reset and paid alert delivery.
4. Create the approved Stripe products/prices for Personal and Business.
5. Exercise Stripe Checkout in test mode.
6. Verify Stripe webhook subscription activation/update/cancel/past-due state.
7. Verify Billing Portal return flow.
8. Exercise SSL/domain expiration and DNS/uptime changes with controlled targets.

## Next build milestone

Launch Phase 6 — public website:

- public homepage
- mission / explanation
- pricing with approved actual prices
- privacy and terms
- help / Learn
- support/contact
- public product-to-signup entry paths

Then:

- search/discovery pages
- dogfooding
- additional thin monitor families

## Lifecycle

Use these states for products:

- Captured
- Validate
- Ready
- Building
- Testing Required
- Released
- Parked
- Rejected
