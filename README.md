# Clarity Belongs

**Clarity belongs to everyone. We help you get there.**

Clarity Belongs is a self-service software ecosystem for understanding what changed, what matters, what needs attention, and what action to take next.

The product identity is intentionally different from a traditional monitoring or watchdog brand. The goal is calm visibility, useful context, and better decisions.

## Ecosystem role

- **Software Belongs — DO**: self-service tools that help people and businesses get things done.
- **Clarity Belongs — KNOW**: monitoring, history, comparison, alerts, and awareness.
- **AutoPilot IT — HANDLE**: low-touch IT services for businesses that want technology handled for them.

## Current V1

Launch phases 1–5 are implemented:

- real account signup/sign-in/sign-out
- personal authenticated My Clarity workspace
- password-reset flow
- Free / Personal / Business membership boundary
- plan-based follow and cadence limits
- Stripe Checkout / Billing Portal / webhook integration boundary
- My Clarity dashboard
- Add Follow wizard
- follow settings/history/evidence
- Website Change Monitor
- Website Uptime Monitor
- SSL Expiration Monitor
- Domain Expiration Monitor
- DNS Change Monitor
- scheduled observations
- in-app alerts
- paid queued SMTP email alerts
- immediate or daily-digest email delivery
- failure/recovery alerts
- SSL/domain expiration thresholds

CI now verifies restore/build/startup plus a real local account flow: signup, authenticated Account access, and authenticated Free-plan follow creation.

Representative real targets, production SMTP, and real Stripe test-mode flows remain **Testing Required**.

## Core platform loop

`source -> observe -> store -> compare -> history -> alert -> user`

Clarity products share this infrastructure rather than becoming independent codebases.

## Run locally

Requirements: .NET 10 SDK.

```text
dotnet restore ClarityBelongs.slnx
dotnet build ClarityBelongs.slnx
dotnet run --project src/ClarityBelongs.Web/ClarityBelongs.Web.csproj
```

SQLite is created automatically on first startup. The app also exposes `/health` for runtime verification.

## Accounts and plans

New accounts receive a personal `My Clarity` workspace and start on Free.

Current limits:

- Free — 5 active follows, 6-hour minimum cadence
- Personal — 50 active follows, 15-minute minimum cadence, email delivery
- Business — 250 active follows, 5-minute minimum cadence, email delivery

The exact public paid prices are intentionally not hard-coded until approved Stripe products/prices are chosen.

See `docs/07-ACCOUNTS-MEMBERSHIP-BILLING.md` for the complete boundary.

## Stripe configuration

Stripe is off by default. Configure production/test values through environment or deployment configuration, not source control.

Expected configuration keys include:

```text
Stripe__Enabled=true
Stripe__SecretKey=...
Stripe__WebhookSecret=...
Stripe__PersonalPriceId=...
Stripe__BusinessPriceId=...
```

Webhook endpoint:

`POST /webhooks/stripe`

The implementation supports Stripe-hosted subscription Checkout, Billing Portal sessions, and subscription state synchronization.

## Email configuration

Email delivery is off by default. Configure the `Email` section through production configuration/environment settings rather than committing credentials.

Supported delivery modes:

- `Immediate`
- `DailyDigest`

In-app alerts remain available on Free. External email delivery requires an active paid membership and configured SMTP provider.

## Product families

- Money
- Your Internet
- Changes
- Opportunities
- Public Information
- Your Identity
- My Clarity

## Principles

- self-service first
- low support burden
- simple, calm UX
- useful free functionality where delivery cost is negligible
- paid features where ongoing delivery has meaningful cost or recurring value
- privacy-respecting by default
- no fear-driven or adversarial branding
- clear history and evidence behind alerts
- reusable engines before one-off products

## Repository structure

- `docs/` — charter, architecture, roadmap, brand, and account/billing decisions
- `portfolio/` — product families, opportunities, and status
- `src/` — runnable product implementation
- `.github/workflows/` — build, runtime, and account verification

## Domains and identity

Primary domain: `claritybelongs.com`

Defensive domain: `claritybelongs.net`

Primary social handle: `@claritybelongs`
