# Search Console Release Handoff

Use this checklist after the Phase 1 SEO release is deployed to `https://claritybelongs.com`.

## Submit the sitemap

1. Open the `claritybelongs.com` property in Google Search Console.
2. Open **Sitemaps**.
3. Submit `https://claritybelongs.com/sitemap.xml`.
4. Confirm the sitemap fetch succeeds and the discovered URL count matches the deployed sitemap.

## Inspect key public URLs

Use **URL inspection** for:

- `https://claritybelongs.com/`
- `https://claritybelongs.com/products`
- `https://claritybelongs.com/products/website-uptime`
- `https://claritybelongs.com/products/http-status`
- `https://claritybelongs.com/products/ssl-expiration`
- `https://claritybelongs.com/products/domain-expiration`
- `https://claritybelongs.com/products/dns-change`
- `https://claritybelongs.com/products/api-endpoint-uptime`
- `https://claritybelongs.com/products/service-outage`
- `https://claritybelongs.com/learn`
- representative Learn URLs from the deployed sitemap

Confirm each intended public URL returns `200`, is indexable, renders the expected title/description, and declares a self-referencing canonical.

## Request indexing

After inspection succeeds, request indexing for:

1. Home.
2. Products.
3. Website Uptime.
4. HTTP Status.
5. SSL Expiration.
6. Domain Expiration.
7. DNS Change.
8. API Endpoint Uptime.
9. Service Outage.
10. The highest-value Phase 1 Learn guides.

Let the sitemap and internal links handle normal discovery for the remaining approved URLs.

## Verify excluded and private surfaces

Inspect a representative set and confirm they are not eligible for indexing:

- `/login`
- `/signup`
- `/forgot-password`
- `/reset-password`
- `/account`
- `/settings`
- `/my-clarity`
- `/add`
- a follow-detail URL
- a change-detail URL
- `/feedback-ops`
- `/reliability-ops`
- `/operational-health`
- a hidden product such as `/products/product-price`
- an unknown product slug
- an unknown Learn slug

Private rendered pages should carry `noindex, nofollow`. Hidden and unknown dynamic routes should return HTTP `404` and must not appear in the sitemap.

## Monitor indexing reports

During the first release weeks, review **Page indexing** and URL inspection for:

- Soft 404.
- Duplicate without user-selected canonical.
- Google chose different canonical than user.
- Crawled - currently not indexed.
- Discovered - currently not indexed.
- Any indexed URL outside the approved Phase 1 sitemap.

An indexed hidden monitor identity, authenticated route, owner/admin/ops route, private API surface, or invalid dynamic route is a release defect and should be removed from indexing rather than added to the sitemap.
