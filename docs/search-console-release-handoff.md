# Search Console Release Handoff

Use this checklist after the Phase 1 SEO release is deployed to `https://claritybelongs.com`.

No structured data was added for this release. The current site does not need synthetic review, rating, FAQ, or organization claims to be indexable.

## Submit the sitemap

1. Open the `claritybelongs.com` property in Google Search Console.
2. Open **Sitemaps**.
3. Submit `https://claritybelongs.com/sitemap.xml`.
4. Confirm the sitemap fetch succeeds and the discovered URL count matches the deployed sitemap.

## Inspect key public URLs

Use **URL inspection** for Home, Products, Website Uptime, HTTP Status, SSL Expiration, Domain Expiration, DNS Change, API Endpoint Uptime, Service Outage, Learn, and representative Learn URLs from the deployed sitemap.

Confirm each intended public URL returns `200`, is indexable, renders the expected title/description, and declares a self-referencing canonical.

## Request indexing

After inspection succeeds, request indexing for Home, Products, the strongest Phase 1 product pages, and the highest-value Phase 1 Learn guides. Let the sitemap and internal links handle normal discovery for the remaining approved URLs.

## Verify excluded and private surfaces

Inspect representative private and excluded URLs including login, signup, password reset, account, settings, My Clarity, follow/change detail, feedback ops, reliability/operational health, a hidden product, an unknown product, and an unknown Learn slug.

Private rendered pages should carry `noindex, nofollow`. Hidden and unknown dynamic routes should return HTTP `404` and must not appear in the sitemap.

## Monitor indexing reports

During the first release weeks, review **Page indexing** and URL inspection for soft 404s, duplicate canonicals, Google-selected canonical differences, crawled/discovered not indexed, and any indexed URL outside the approved Phase 1 sitemap.

An indexed hidden monitor identity, authenticated route, owner/admin/ops route, private API surface, or invalid dynamic route is a release defect and should be removed from indexing rather than added to the sitemap.
