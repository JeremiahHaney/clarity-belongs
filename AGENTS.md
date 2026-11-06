# Clarity Belongs — Coding Agent Entry Point

**Read [AI_CONTEXT.md](AI_CONTEXT.md) first.** This file is for Codex and other agents that recognize `AGENTS.md`; repository-level instructions take precedence over broad portfolio summaries when operating on code. Follow any narrower `AGENTS.md` that exists in a working subtree.

## Source of portfolio context
The strategy and product-identity authority is [free-software-for-the-people/00_AI_CONTEXT.md](https://github.com/JeremiahHaney/free-software-for-the-people/blob/main/00_AI_CONTEXT.md), with `product-atlas.yaml` and `product-identities.json` for exact cross-brand IDs. Do not assume the portfolio strategy repo contains this application's code. Product-implementation truth belongs here.

## Before proposing or editing features
1. Read this repository's `AGENTS.md`, `AI_CONTEXT.md`, target subsystem docs, **latest changelog entry/version for each affected project**, source and tests.
2. Search the Atlas by product ID/alias and this repository by feature, route, service, models, tests, and prior changelog before calling something missing.
3. Separate Released, Verified, Built / Testing Required, Partial, Planned and Unknown. Unknown is not missing; a database model, API, doc or merged commit is not production proof.
4. Prefer extending, linking, verifying, QA-ing or publishing an existing feature. Don't reinvent a platform with a speculative backlog.
5. Record exact location, dependency boundaries, acceptance test, safety implications and smallest viable diff. Run meaningful tests before claiming validation. Increment project version using its **latest actual changelog entry**, not a remembered version.
6. Update relevant status/docs for actual changes; update portfolio Atlas only for durable identity/dependency/strategic changes. Avoid duplicating the Atlas in prose.

