# SPF Record Monitor

## Product facts
- Slug: `spf-record`
- Public URL: https://claritybelongs.com/products/spf-record
- Product type: SPF-related TXT DNS watch
- Current state: **Working V1 / QA Required**
- Product model: capability inside the shared Clarity monitoring service, not a separate company/app
- Core workflow: sign in -> Watch Something -> provide a public target -> Clarity observes it on a schedule -> history records meaningful state/change evidence -> user reviews it in My Clarity
- Promise: Published SPF-related TXT evidence changes and later troubleshooting lacks history.
- User receives: persisted observation/history evidence and relevant change/failure/recovery state
- Account required: yes for hosted follows
- Data boundary: observes the public target supplied by the user; hosted follow/history data is stored remotely
- Current public cost: Free-only launch configuration; paid billing remains gated

## Problem and audience
**Problem:** Published SPF-related TXT evidence changes and later troubleshooting lacks history.

**Primary audiences:** email administrators, marketers with technical ownership, domain operators

**Job to be done:** "When sender authorization changes, I want the public SPF-related TXT evidence preserved so I can see what changed."

## Search intent
Current intent cluster to validate and expand with Search Console after launch:
- SPF record monitor
- monitor SPF changes
- SPF DNS history

Do not manufacture volume numbers. Use actual query/impression data once Search Console has enough traffic.

## Alternatives / competitors
DMARC/email security tools, DNS monitoring suites, dig/manual checks. The behavioral alternative is manual checking; the broad substitute is a general automation/monitoring platform.

## Demonstration use cases
1. ESP added to SPF
2. SPF policy accidentally replaced
3. migration changes include chain

Demonstrations must use safe public targets and show what Clarity actually records.

## Distribution map
Search/Learn; email operations communities with strict non-promotional participation; technical content

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
Share public DNS evidence; do not claim Clarity validates deliverability.
Sharing is optional and must strip account/private identifiers. A recipient should be able to understand the source, timestamp, and observed state before following the product link.

## Readiness gate
Before external promotion: clean-browser production QA, successful follow creation, persisted observation, useful history, mobile check, error path, privacy/contact/feedback, analytics/UTM attribution, screenshot/share asset, and claim-vs-implementation review.
