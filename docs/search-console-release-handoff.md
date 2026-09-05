# Search Console Release Handoff

Use this checklist after the Phase 1 SEO build is deployed to `https://claritybelongs.com`.

## 1. Submit the sitemap

1. Open the `claritybelongs.com` property in Google Search Console.
2. Open **Sitemaps**.
3. Submit `https://claritybelongs.com/sitemap.xml`.
4. Confirm Search Console accepts it without fetch or parse errors.
5. Verify the discovered URL count matches the deployed Phase 1 sitemap.

## 2. Inspect key public URLs

Use **URL inspection** against the deployed site for:

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
- each URL present in the deployed Learn portion of the sitemap

For each URL confirm:

- HTTP status is `200`.
- Google-selected canonical is expected to become the self-referencing canonical.
- the page is allowed to be indexed.
- rendered HTML contains the expected title and description.

## 3. Request indexing

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
10. The highest-value Phase 1 Learn pages.

The remaining approved sitemap URLs can be discovered through the sitemap and internal links rather than manually requesting every URL at once.

## 4. Verify excluded/private surfaces

Inspect a representative set of private or excluded URLs and confirm they are not eligible for indexing:

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
- one hidden product slug such as `/products/product-price`
- one unknown product slug
- one unapproved or unknown Learn slug

Private application pages should render `noindex, nofollow` when rendered. Hidden and unknown public dynamic slugs should return HTTP `404` and must not appear in the sitemap.

## 5. Monitor indexing reports

During the first release weeks, review **Page indexing** and URL inspection for:

- Soft 404 reports.
- Duplicate without user-selected canonical.
- Google chose different canonical than user.
- Crawled - currently not indexed.
- Discovered - currently not indexed.
- Indexed URLs that are not present in the approved Phase 1 sitemap.

Any hidden monitor identity, private route, feedback-ops route, authenticated surface, or invalid dynamic route appearing as indexed is a release defect and should be removed from indexing rather than added to the sitemap.
