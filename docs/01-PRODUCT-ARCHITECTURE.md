# Product Architecture

Clarity should feel like one coherent awareness product with many entry points, not a pile of unrelated monitors.

```text
CLARITY BELONGS
|
+-- MONEY
|   +-- Price tracking
|   +-- Price & sale history
|   +-- Fee changes
|   +-- Subscription changes
|   +-- Airline fares
|   +-- Hotel/rental rates
|   +-- Ticket prices
|   +-- Product availability
|   +-- Restock & availability
|
+-- YOUR INTERNET
|   +-- Domain expiration
|   +-- SSL expiration
|   +-- DNS changes
|   +-- Email configuration
|   +-- Blacklist/reputation status
|   +-- Website availability
|   +-- Website performance
|
+-- CHANGES
|   +-- Webpage changes
|   +-- Terms changes
|   +-- Privacy-policy changes
|   +-- Product changes
|   +-- Software releases
|   +-- Competitor changes
|   +-- Service outages
|   +-- Cancellations & schedule changes
|
+-- OPPORTUNITIES
|   +-- Jobs
|   +-- Grants
|   +-- Government bids
|   +-- Availability
|   +-- New listings
|   +-- Deadlines
|
+-- PUBLIC INFORMATION
|   +-- Public meeting agendas
|   +-- Government changes
|   +-- Regulatory filings
|   +-- Policy changes
|   +-- Public records
|   +-- Recall alerts
|   +-- Local government projects
|   +-- School & community notices
|   +-- Consumer notices
|
+-- YOUR IDENTITY
|   +-- Brand mentions
|   +-- Username impersonation
|   +-- Typo domains
|   +-- Domain registrations
|   +-- Reputation changes
|
+-- MY CLARITY
    +-- Everything I follow
    +-- Changes since last visit
    +-- Important alerts
    +-- History
    +-- Before/after comparison
    +-- Notification settings
```

## Discovery model

Users should be able to enter through a specific problem such as "track SSL expiration," "tell me when this product comes back," or "watch this public deadline page" while all products feed the same My Clarity experience.

The homepage should expose families and outcomes, not hundreds of individual product tiles.

## V1 product rule

A product can ship as a thin Clarity surface when an existing observation adapter can truthfully answer the user's problem. Public-source products should clearly state that V1 watches the selected public page or endpoint and should not imply structured extraction, private-account access, authoritative external datasets, or integrations that have not been built.
