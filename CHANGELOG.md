# Changelog

## 0.6.5 - 2026-09-04

- Added `portfolio/desktop.md` to define Clarity Belongs' focused desktop strategy for private, local, large, or awkward-to-upload documents.
- Defined a 10-product desktop slate centered on Clarity Reader, Document Compare, Local OCR, redaction, privacy checking, local search, organization, batch processing, transcript cleanup, and the combined Clarity Desktop shell.
- Locked the identity boundary: Clarity desktop is for understanding and working with private/local documents, while generic utilities remain in Software Belongs and device diagnosis/support remains in AutoPilot IT.
- Defined a shared-engine approach so Clarity can reuse common Belongs desktop, OCR, indexing, document, and filesystem capabilities without duplicating code.

## 0.6.4 - 2026-09-03

- Simplified Clarity around one clear customer promise: tell Clarity what matters and it keeps an eye on it for you.
- Rewrote the homepage to lead with things customers care about instead of individual monitor implementations.
- Reframed the public catalog as monitoring capabilities underneath one Clarity service rather than dozens of separate apps.
- Renamed public navigation from Products to What It Watches and updated the brand/footer language around the simplified service model.
- Added `docs/clarity-product-direction.md` to lock the distinction between Clarity observation/history and Software Belongs consumer-action tools.

## 0.6.3 - 2026-09-03

- Reworked the authenticated Clarity navigation around My Clarity, Watch Something, Products, Learn, and Account so the signed-in experience behaves like an application rather than a marketing site.
- Rebuilt `/add` as a guided Watch Something experience with clearer hierarchy, grouped monitor selection, stronger target input, plan context, and a more product-like two-column layout.
- Added a Website Essentials watch pack that accepts one website/domain and creates Website Uptime, Website Change, SSL Expiration, Domain Expiration, and DNS Change follows together.
- Kept watch packs on the existing Follow and monitoring engines so the new UX does not introduce a separate execution model.
- Added plan-capacity validation before creating a pack so Free-plan users do not begin a pack they cannot finish.
- Added post-creation success guidance and related-monitor recommendations so one configured watch naturally leads to useful next watches.
- Added target query prefill support for recommendation links and expanded responsive styling for the new watch flow.

## 0.6.2 - 2026-09-03

- Added a development-only localhost `/dev/login` route for quickly exploring authenticated Clarity flows without storing temporary credentials in the repository.
- The route creates or reuses a local `Clarity Explorer` Free-plan account, signs it in for seven days, and redirects directly to `/my-clarity`.
- Restricted the helper to the Development environment and loopback requests so it is unavailable in Production deployments.

## 0.1.0 - 2026-09-03

- Established Clarity Belongs as the ecosystem's KNOW identity.
- Locked the mission: clarity belongs to everyone; help people see what changed, understand what matters, and make better decisions.
- Defined the initial product families: Money, Your Internet, Changes, Opportunities, Public Information, Your Identity, and My Clarity.
- Defined the shared observation loop: source -> observe -> store -> compare -> history -> alert -> user.
- Added the initial opportunity backlog and roadmap for a reusable monitoring/awareness platform.
- Recorded the primary and defensive domains and the shared @claritybelongs social identity.
