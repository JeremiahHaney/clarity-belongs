# Distribution priorities

Priority reflects current public readiness, demand clarity, demonstration value, mission fit, trust risk, and ability to teach us about the whole service. It does **not** reward sunk development effort.

## Priority 0 — prove launch readiness
Audit status: **not yet cleared**. See `distribution/READINESS_AUDIT.md`.

Completed in the 2026-09-19 audit:
- public Watch Something is restricted to the approved public catalog
- product intent survives signup and returns the user to the selected watch
- privacy/contact/feedback/owner-operations paths are present
- mobile and public-copy regression contracts are present
- persistence/history/failure/recovery have behavioral coverage
- first-party UTM attribution and funnel reporting are implemented

Remaining blockers:
1. Run the production account/follow flow from a clean browser and verify tagged attribution in Owner Operations.
2. Capture final desktop/mobile launch screenshots for the first five watches and Website Essentials. Branded share assets are now committed and wired.
3. Re-run Release tests and deployed smoke on the final readiness commit.

## First promotion cohort
These should be tested first after Priority 0 passes:
- Website Change Monitor
- Website Uptime Monitor
- SSL Expiration Monitor
- Domain Expiration Monitor
- DNS Change Monitor

Why this cohort: the problems are easy to explain, search intent is concrete, demonstrations are simple, and the same website/domain target can expose Clarity's "one thing, several useful watches" model.

## Second cohort
- HTTP Status Monitor
- Redirect Destination Monitor
- Broken Link Monitor
- API Endpoint Uptime Monitor
- Service Outage Monitor

These fit developer/ops audiences and can reuse technical community distribution, but should follow a clean reliability-focused landing/demo.

## Third cohort
- Nameserver, MX, SPF, DKIM, and DMARC monitors

These are useful and differentiated as historical evidence, but narrower and more technical. Promote through domain/email-operations content rather than generic launch channels first.

## Separate tracks
- **Learn:** distribute as high-intent educational content tied to real product workflows.
- **Free Local Tools:** keep separate from the hosted monitoring launch; test only where a specific utility solves a community problem.
- **Clarity Desktop:** Hold. The current repo contains portfolio definitions, not releasable desktop products.

## Explicit hold
Do not promote hidden/internal catalog identities as standalone products. They are backlog/use-case definitions until deliberately approved for public release.
