# Website Change Monitor

## Product facts
- Slug: `website-change`
- Public URL: https://claritybelongs.com/products/website-change
- Product type: public webpage change watch
- Current state: **Working V1 / QA Required**
- Product model: capability inside the shared Clarity monitoring service, not a separate company/app
- Core workflow: sign in -> Watch Something -> provide a public target -> Clarity observes it on a schedule -> history records meaningful state/change evidence -> user reviews it in My Clarity
- Promise: Repeatedly checking a public page to notice whether its content changed.
- User receives: persisted observation/history evidence and relevant change/failure/recovery state
- Account required: yes for hosted follows
- Data boundary: observes the public target supplied by the user; hosted follow/history data is stored remotely
- Current public cost: Free-only launch configuration; paid billing remains gated

## Problem and audience
**Problem:** Repeatedly checking a public page to notice whether its content changed.

**Primary audiences:** site owners, researchers, consumers, small teams

**Job to be done:** "When a public page matters, I want a dated before/after history so I can notice changes without manually revisiting it."

## Search intent
Current intent cluster to validate and expand with Search Console after launch:
- website change monitor
- monitor webpage changes
- get notified when webpage changes

Do not manufacture volume numbers. Use actual query/impression data once Search Console has enough traffic.

## Alternatives / competitors
Visualping, Distill, changedetection.io, browser bookmarks/manual checking, general automation tools. The behavioral alternative is manual checking; the broad substitute is a general automation/monitoring platform.

## Demonstration use cases
1. pricing page changes
2. terms/privacy page changes
3. school or government notice page changes

Demonstrations must use safe public targets and show what Clarity actually records.

## Distribution map
Search/Learn; Product Hunt/AlternativeTo/SaaSHub/IndieNeed as part of Clarity; r/devops feedback thread where technically relevant

Shared exact directory/community actions live in `distribution/data/actions.csv` and the campaign files. Directory launches should promote **Clarity Belongs as one service**, not create duplicate listings for every monitor.

## Content
- Search article answering the highest-intent query above
- short demo showing baseline -> changed/failure state -> history
- comparison/explainer that distinguishes this watch from adjacent Clarity watches
- troubleshooting guide for the most likely target/configuration failure
- concrete before/after example with truthful limitations

## Trust and transparency
- Show source/target and observation timestamp.
- Separate observed facts from any interpretation.
- Preserve uncertainty and failure states rather than fabricating a result.
- Do not imply private access, universal crawling, root-cause knowledge, or objective truth.
- Explain what is stored and what remains public.
- Keep user control over pause/resume/archive and target choice.

## Share loop
Share a sanitized before/after change record only after privacy review.
Sharing is optional and must strip account/private identifiers. A recipient should be able to understand the source, timestamp, and observed state before following the product link.

## Readiness gate
Before external promotion: clean-browser production QA, successful follow creation, persisted observation, useful history, mobile check, error path, privacy/contact/feedback, analytics/UTM attribution, screenshot/share asset, and claim-vs-implementation review.
