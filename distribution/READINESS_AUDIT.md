# Priority 0 readiness audit — 2026-09-19

Scope: Website Change, Website Uptime, SSL Expiration, Domain Expiration, and DNS Change.

## Result
**Not ready for external promotion yet.** The five-watch product cohort is structurally ready enough for final production validation, but attribution and live stranger-flow proof are still blockers.

## Audit matrix

| Gate | Status | Evidence / finding |
|---|---|---|
| Public product routes | Pass | All five are in the explicit 15-watch public catalog and sitemap. |
| Claims match implementation | Pass with regression coverage | Public launch tests ban unsupported development/filtering claims; Website Change explicitly describes whole-page comparison. |
| Signup/login | Pass in code | Hardened auth endpoints, CSRF, throttling, cookies, and account tests exist. |
| Product -> signup -> selected watch | **Fixed in this audit** | Product signup now carries a safe local return URL to the intended `/add?product=...` flow. |
| Watch Something public boundary | **Fixed in this audit** | `/add` now uses only `PublicClarityProductCatalog`; hidden/internal monitors and hidden recommendations are no longer selectable by ordinary users. |
| Five-watch Website Essentials pack | Pass in code | Pack contains Website Uptime, Website Change, SSL Expiration, Domain Expiration, and DNS Change with plan-capacity validation. |
| Follow persistence/history | Pass by implementation/tests | Durable follow, snapshot, change, and restart coverage exists; follow detail exposes history and before/after evidence. |
| Failure/recovery behavior | Pass by behavioral coverage | HTTP/TLS/RDAP/DNS behavioral tests cover failed observations and recovery transitions. |
| Mobile/responsive | Pass by regression contract | Launch tests require desktop/tablet/mobile nav breakpoints; watch layout has responsive breakpoints. |
| Privacy | Pass in repo | Public `/privacy` route exists and is linked from the site shell. |
| Contact | Pass in repo | Public `/contact` form exists. |
| Product feedback | Pass in repo | Public `/feedback` form and protected owner/operations review path exist. |
| Owner visibility | Pass in repo | Owner console exposes users, follows, failures, notifications, worker state, feedback, and contact messages. |
| Search metadata / sitemap / robots | Pass by tests | Explicit public catalog, canonical/indexability policy, sitemap, and robots rules exist. |
| First-party acquisition attribution | **BLOCKER** | No implementation for `utm_source`, `utm_medium`, `utm_campaign`, or equivalent acquisition-event persistence was found. |
| Funnel measurement | **BLOCKER** | No durable source -> visit -> follow start -> follow created -> useful observation -> return measurement was found. |
| Screenshots/share assets | **BLOCKER / not verified** | No durable launch screenshot/share-asset set was identified in the audited repo paths. |
| Live production stranger-flow | **BLOCKER / not externally verified** | The production site could not be fetched from the available external web environment, so live signup/follow/observation/mobile behavior is not certified by this audit. |

## Code changes made
1. Restricted `/add` to the approved public catalog.
2. Removed hidden Sitemap/Robots recommendations from the Website Essentials success flow.
3. Removed hidden-product fallback recommendations from the public watch flow.
4. Preserved selected-product intent across account creation using a server-validated local return URL.
5. Added launch regression coverage for the public Watch Something boundary and product-signup continuity.

## Remaining Priority 0 work
1. Implement minimal first-party acquisition/funnel measurement.
2. Validate the deployed production flow from a clean browser:
   `landing -> product -> signup -> selected watch -> create follow -> first observation -> history`.
3. Capture one desktop and one mobile screenshot for each of the five first-cohort watches plus one Website Essentials pack screenshot.
4. Re-run Release build/tests and production smoke against the commit containing the fixes.
5. Only then mark the first cohort Ready in `distribution/data/actions.csv`.

## Promotion decision
Do not execute external promotion yet.
