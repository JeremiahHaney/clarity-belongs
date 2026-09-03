# Clarity Belongs

**Clarity belongs to everyone. We help you get there.**

Clarity Belongs is a self-service software ecosystem for understanding what changed, what matters, what needs attention, and what action to take next.

The product identity is intentionally different from a traditional monitoring or watchdog brand. The goal is calm visibility, useful context, and better decisions.

## Ecosystem role

- **Software Belongs — DO**: self-service tools that help people and businesses get things done.
- **Clarity Belongs — KNOW**: monitoring, history, comparison, alerts, and awareness.
- **AutoPilot IT — HANDLE**: low-touch IT services for businesses that want technology handled for them.

## Current V1

Launch phases 1–4 are implemented:

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
- queued SMTP email alerts
- immediate or daily-digest email delivery
- failure/recovery alerts
- SSL/domain expiration thresholds

The interactive product flows and production SMTP provider remain **Testing Required** until exercised against representative real targets and production configuration.

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

## Email configuration

Email delivery is off by default. Configure the `Email` section through production configuration/environment settings rather than committing credentials.

Supported delivery modes:

- `Immediate`
- `DailyDigest`

The user-facing notification email can be changed from Clarity Settings. In-app alerts remain available independently of external email delivery.

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

- `docs/` — charter, architecture, roadmap, and brand decisions
- `portfolio/` — product families, opportunities, and status
- `src/` — runnable product implementation
- `.github/workflows/` — build and runtime verification

## Domains and identity

Primary domain: `claritybelongs.com`

Defensive domain: `claritybelongs.net`

Primary social handle: `@claritybelongs`
