# API Endpoint Uptime Monitor

## Product facts
- Slug: `api-endpoint-uptime`
- Public URL: https://claritybelongs.com/products/api-endpoint-uptime
- Product type: public API/health endpoint availability watch
- Current state: **Working V1 / QA Required**
- Product model: capability inside the shared Clarity monitoring service, not a separate company/app
- Core workflow: sign in -> Watch Something -> provide a public target -> Clarity observes it on a schedule -> history records meaningful state/change evidence -> user reviews it in My Clarity
- Promise: A safe public API or health endpoint becomes unavailable and teams need simple history.
- User receives: persisted observation/history evidence and relevant change/failure/recovery state
- Account required: yes for hosted follows
- Data boundary: observes the public target supplied by the user; hosted follow/history data is stored remotely
- Current public cost: Free-only launch configuration; paid billing remains gated

## Problem and audience
**Problem:** A safe public API or health endpoint becomes unavailable and teams need simple history.

**Primary audiences:** developers, small SaaS teams, operators

**Job to be done:** "When a public endpoint represents a service, I want outage/recovery history without running a full observability stack."

## Search intent
Current intent cluster to validate and expand with Search Console after launch:
- API uptime monitor
- monitor public API endpoint
- API health check monitor

Do not manufacture volume numbers. Use actual query/impression data once Search Console has enough traffic.

## Alternatives / competitors
Better Stack, UptimeRobot, Pingdom, Datadog synthetics, curl/cron. The behavioral alternative is manual checking; the broad substitute is a general automation/monitoring platform.

## Demonstration use cases
1. public health endpoint outage
2. status endpoint returns failure
3. recovery after deploy

Demonstrations must use safe public targets and show what Clarity actually records.

## Distribution map
Search/Learn; r/devops weekly thread; Show HN; developer directories

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
Share only public endpoint history; authenticated/private endpoints are outside scope.
Sharing is optional and must strip account/private identifiers. A recipient should be able to understand the source, timestamp, and observed state before following the product link.

## Readiness gate
Before external promotion: clean-browser production QA, successful follow creation, persisted observation, useful history, mobile check, error path, privacy/contact/feedback, analytics/UTM attribution, screenshot/share asset, and claim-vs-implementation review.
