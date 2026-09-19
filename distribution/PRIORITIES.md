# Distribution priorities

Priority reflects current public readiness, demand clarity, demonstration value, mission fit, trust risk, and ability to teach us about the whole service. It does **not** reward sunk development effort.

## Priority 0 — prove launch readiness
1. Production/account/follow smoke test from a stranger's perspective.
2. Verify first-party attribution and funnel events.
3. Capture screenshots/share assets.
4. Verify contact/feedback handling and owner operations.
5. Verify all 15 watch pages make claims that match implementation.

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
