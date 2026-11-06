# Clarity Belongs — AI Quick Context

> Evidence-backed entry point. Not a product release ledger or replacement for code. Reviewed 2026-11-06; check newer commits/changelogs before work.

## Mission and boundary
KNOW: source observation, history, changes, alerts and actionable understanding.

## Source-of-truth reading order
- `README.md`
- `docs/07-ACCOUNTS-MEMBERSHIP-BILLING.md`
- `docs/08-PUBLIC-SITE-SEARCH-DISCOVERY.md`
- `ClarityBelongs.slnx`

## Main implementation areas (navigate, verify exact paths before edits)
- `src/`
- `docs/`

## Existing capability and gaps
README documents 64 V1 monitor entry points reusing HTTP/TLS/DNS/RDAP observation primitives, account/My Clarity, alerts, plan and Stripe boundaries. Build, startup, acquisition and Free-plan follow tests are described; live SMTP, Stripe real flows, deployment and representative targets require validation. Do not propose one engine per monitor.

## What to do first on a task
1. Identify target product and Atlas key; consult status docs and source/route/tests.
2. Find current implementation and latest changelog/version. Search for existing behavior before designing alternatives.
3. State what **already exists**, what **needs verification/integration**, and what is **actually missing**, with file evidence.
4. Make small changes in owning projects; preserve boundaries; run checks; update changelog/status.

## Portfolio context
[Portfolio AI index](https://github.com/JeremiahHaney/free-software-for-the-people/blob/main/00_AI_CONTEXT.md) · [Product Atlas](https://github.com/JeremiahHaney/free-software-for-the-people/blob/main/product-atlas.yaml)

## Mechanically discovered capability evidence
The central portfolio repo's `scripts/build-ai-capability-index.py` can produce a **local, evidence-only index** of tracked projects, routes, API declarations, changelog paths and tests for sibling checkouts. These are source references, not certified capabilities; a human or verified automated test must assign release status.
