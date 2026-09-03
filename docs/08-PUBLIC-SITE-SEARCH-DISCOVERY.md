# Public Site + Search & Discovery

## Purpose

Launch Phases 6 and 7 turn the working Clarity application into a public product people can understand and discover.

The public acquisition loop is:

`search/problem -> Learn or product page -> free account -> Follow -> My Clarity -> recurring value`

Software Belongs can also act as an upstream DO entry point and refer recurring awareness problems into Clarity.

## Public site map

- `/` — public Clarity Belongs homepage
- `/products` — first monitor catalog
- `/products/{slug}` — dedicated product surfaces
- `/pricing` — Free / Personal / Business membership explanation
- `/learn` — problem-first discovery library
- `/learn/{slug}` — search-intent guide pages
- `/about` — mission and DO / KNOW / HANDLE ecosystem
- `/support` — self-service help
- `/privacy` — draft public-V1 privacy policy
- `/terms` — draft public-V1 terms
- `/login` / `/signup` — account entry
- `/my-clarity` — authenticated awareness dashboard

## Initial search intents

The first Learn catalog intentionally targets problems already served by the current five monitors:

- monitor website changes
- notify me when a webpage changes
- website uptime monitor
- SSL expiration alert
- domain expiration reminder
- DNS change monitor
- track pricing page changes
- monitor terms and privacy policy changes
- monitor public notice page
- what is website change monitoring
- website change monitor vs uptime monitor
- how often should a website monitor check

This keeps discovery honest: every public page points to functionality that already exists rather than advertising speculative monitors.

## Page rules

Every problem-first page should:

1. answer the searcher's question plainly,
2. explain what Clarity actually does,
3. preserve the distinction between factual observation and interpretation,
4. link to one relevant product,
5. offer the Free plan without manipulative urgency,
6. link to related guides where useful.

## Search infrastructure

- `robots.txt` allows public pages and excludes authenticated/private application routes.
- `sitemap.xml` lists the public homepage, product pages, legal/help pages, and initial Learn entries.
- product and article pages provide unique page titles and description metadata.
- internal links connect homepage -> products -> Learn -> signup / Add Follow.

## Software Belongs connection

Clarity is the KNOW layer. Software Belongs remains the DO layer.

Useful future cross-links should follow the pattern:

`Software Belongs free checker / how-to -> "Want us to keep watching this?" -> Clarity Belongs`

Examples:

- SSL checker -> SSL Expiration Monitor
- DNS utility -> DNS Change Monitor
- website checker -> Website Uptime Monitor
- page comparison utility -> Website Change Monitor

Those Software Belongs changes are outside this repository and should be coordinated there rather than hard-coded from Clarity.

## Release status

The public site and initial search/discovery surfaces are implemented in the Clarity repo. They remain Testing Required for browser presentation, mobile behavior, final legal copy, production domain deployment, analytics/search-console setup, and live indexing.
