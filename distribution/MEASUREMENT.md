# Distribution measurement

Clarity uses minimal first-party acquisition measurement. No third-party analytics SDK is required.

## Supported campaign parameters
- `utm_source`
- `utm_medium`
- `utm_campaign`

Example:

`https://claritybelongs.com/products/website-change?utm_source=reddit&utm_medium=community&utm_campaign=priority-zero`

## Funnel events
1. Visit
2. SignupCompleted
3. FollowStarted
4. FollowCreated
5. Useful observation — derived from the existing successful observation history for the created follow
6. Return user — a My Clarity open at least 24 hours after a created follow

## Privacy boundary
Clarity stores:
- a random first-party visitor identifier
- campaign parameters
- path
- authenticated user/workspace IDs after account association
- product/follow IDs for funnel events
- timestamps

Clarity does not store IP addresses, advertising identifiers, or browser fingerprints for this measurement, and the events are not sent to a third-party analytics service.

## Where to review
Owner Operations shows the rolling 30-day funnel grouped by source, medium, and campaign.

## Launch convention
Every external distribution action should use a stable campaign tag. Prefer:
- source: platform/community name, e.g. `reddit`, `producthunt`, `hackernews`
- medium: channel type, e.g. `community`, `directory`, `launch`, `social`
- campaign: durable initiative, e.g. `priority-zero`, `website-monitoring-launch`

Do not use personal names, email addresses, or sensitive information in UTM values.
