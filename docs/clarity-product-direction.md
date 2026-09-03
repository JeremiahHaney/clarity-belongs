# Clarity product direction

Clarity should feel like one simple service, not a catalog of dozens of separate monitoring products.

## Core promise

**Tell Clarity what matters. Clarity keeps an eye on it for you.**

Clarity watches public information over time, remembers what it saw, and shows you what changed.

## Customer experience

Customers should start with the thing they care about rather than choosing an implementation-specific monitor type.

Examples:

- a website
- a domain
- a company
- a product or price
- a trip
- a public notice
- an opportunity
- a service or endpoint

Clarity can select and combine the appropriate underlying monitor types automatically.

## Product model

The existing monitor catalog remains the internal capability layer. Individual monitor types should not dominate the public navigation or onboarding experience.

Public concepts:

- **My Clarity** — everything the customer is already following
- **Watch Something** — the primary entry point for adding something new
- **Alerts** — important changes that need attention
- **History** — what Clarity observed and how it changed over time
- **Learn** — explanations and help

Underlying monitor types should be presented only when useful for advanced configuration, transparency, or detailed product documentation.

## Watch packs

Clarity should infer useful groups of monitors from the target. For example, a website can automatically include uptime, content change, SSL, domain expiration, and DNS monitoring.

The same pattern should expand to trips, products, companies, public information, identity/reputation, opportunities, and operational services.

## Relationship to Software Belongs

Clarity observes and remembers. Software Belongs helps consumers understand information and take action.

Example: Clarity can collect airfare observations over time, while Software Belongs can use those observations for true-trip-cost comparisons, fee normalization, price-pattern analysis, and consumer guidance.
