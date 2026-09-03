# Clarity Belongs

**Clarity belongs to everyone. We help you get there.**

Clarity Belongs is a self-service awareness platform for understanding what changed, what matters, what needs attention, and what action to take next.

## Ecosystem role

- **Software Belongs — DO**: self-service tools that help people and businesses get things done.
- **Clarity Belongs — KNOW**: monitoring, history, comparison, alerts, and awareness.
- **AutoPilot IT — HANDLE**: low-touch IT services for businesses that want technology handled for them.

## Current V1

Launch phases 1–7 are implemented in the repository.

The working product includes:

- public homepage and product-discovery site
- public Learn/search library with 12 initial problem-first guides
- About, Support, Privacy, and Terms surfaces
- robots and sitemap discovery files
- real account signup/sign-in/sign-out
- personal authenticated My Clarity workspace at `/my-clarity`
- password-reset flow
- Free / Personal / Business membership boundary
- plan-based follow and cadence limits
- Stripe Checkout / Billing Portal / webhook integration boundary
- Website Change Monitor
- Website Uptime Monitor
- SSL Expiration Monitor
- Domain Expiration Monitor
- DNS Change Monitor
- scheduled observations
- history and before/after evidence
- in-app alerts
- paid queued SMTP email alerts
- Immediate or DailyDigest email delivery
- failure/recovery alerts
- SSL/domain expiration thresholds

CI verifies restore/build/startup, public acquisition routes, authenticated My Clarity/Account access, and authenticated Free-plan follow creation.

Representative real targets, production SMTP, real Stripe test-mode flows, final legal/contact details, and production deployment remain **Testing Required**.

## Public acquisition loop

`search/problem -> Learn or product page -> free account -> Follow -> My Clarity -> recurring value`

The initial discovery library deliberately covers only problems supported by existing V1 monitors, including website changes, webpage alerts, uptime, SSL expiration, domain expiration, DNS changes, pricing pages, public notices, terms/privacy pages, monitor cadence, and change-vs-uptime comparisons.

See `docs/08-PUBLIC-SITE-SEARCH-DISCOVERY.md`.

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

SQLite is created/upgraded automatically on startup. `/health` is available for runtime verification.

## Accounts and plans

New accounts receive a personal My Clarity workspace and start on Free.

- Free — 5 active follows, 6-hour minimum cadence
- Personal — 50 active follows, 15-minute minimum cadence, email delivery
- Business — 250 active follows, 5-minute minimum cadence, email delivery

Exact public paid prices remain intentionally unapproved/unconfigured rather than being invented in source.

See `docs/07-ACCOUNTS-MEMBERSHIP-BILLING.md`.

## Production integrations

Stripe and SMTP are disabled by default and use environment/deployment configuration rather than committed credentials.

Stripe supports hosted subscription Checkout, Billing Portal sessions, and signed subscription-state webhooks at `POST /webhooks/stripe`.

Email supports `Immediate` and `DailyDigest`. In-app alerts remain available independently; external email requires a paid entitlement and configured provider.

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
- calm UX
- useful free functionality where delivery cost is negligible
- paid features where ongoing delivery has meaningful cost or recurring value
- privacy-respecting by default
- no fear-driven branding
- clear history and evidence behind alerts
- reusable engines before one-off products

## Domains and identity

Primary domain: `claritybelongs.com`

Defensive domain: `claritybelongs.net`

Primary social handle: `@claritybelongs`
