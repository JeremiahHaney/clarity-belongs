#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${1:-http://127.0.0.1:5080}"
TMP_DIR="${TMPDIR:-/tmp}/clarity-public-catalog-tests"
mkdir -p "$TMP_DIR"

PUBLIC_SLUGS=(
  website-uptime
  http-status
  redirect-chain
  broken-link
  ssl-expiration
  domain-expiration
  dns-change
  nameserver-change
  mx-record
  spf-record
  dkim-record
  dmarc-record
  api-endpoint-uptime
  service-outage
  website-change
)

HIDDEN_SLUGS=(
  product-price
  airfare
  competitor-pricing
  public-status-page
  cloud-service-outage
  webhook-health
  cron-heartbeat
)

FORBIDDEN_COPY_REGEX='\bV1\b|\btesting\b|\bproduction release gate\b|\bcan be added later\b|\bsmart filtering\b|\bmeaningful differences\b|\blater\b'

fetch_200() {
  local path="$1"
  local output="$2"
  local status
  status="$(curl --silent --show-error --location --output "$output" --write-out '%{http_code}' "$BASE_URL$path")"
  if [[ "$status" != "200" ]]; then
    echo "Expected 200 for $path, got $status" >&2
    exit 1
  fi
}

assert_404_noindex() {
  local path="$1"
  local name="$2"
  local output="$TMP_DIR/$name.html"
  local status
  status="$(curl --silent --show-error --output "$output" --write-out '%{http_code}' "$BASE_URL$path")"
  if [[ "$status" != "404" ]]; then
    echo "Expected 404 for $path, got $status" >&2
    exit 1
  fi
  grep -Eqi '<meta[^>]+name="robots"[^>]+noindex|<meta[^>]+content="noindex' "$output"
}

fetch_200 /products "$TMP_DIR/products.html"

for slug in "${PUBLIC_SLUGS[@]}"; do
  grep -q "href=\"/products/$slug\"" "$TMP_DIR/products.html"
  fetch_200 "/products/$slug" "$TMP_DIR/product-$slug.html"
  grep -q "rel=\"canonical\" href=\"https://claritybelongs.com/products/$slug\"" "$TMP_DIR/product-$slug.html"

  if grep -Eqi "$FORBIDDEN_COPY_REGEX" "$TMP_DIR/product-$slug.html"; then
    echo "Forbidden launch wording found on /products/$slug" >&2
    grep -Ein "$FORBIDDEN_COPY_REGEX" "$TMP_DIR/product-$slug.html" >&2 || true
    exit 1
  fi
done

public_link_count="$(grep -o 'href="/products/[a-z0-9-]*"' "$TMP_DIR/products.html" | sort -u | wc -l | tr -d ' ')"
if [[ "$public_link_count" != "${#PUBLIC_SLUGS[@]}" ]]; then
  echo "Expected ${#PUBLIC_SLUGS[@]} unique public product links, found $public_link_count" >&2
  exit 1
fi

for slug in "${HIDDEN_SLUGS[@]}"; do
  if grep -q "href=\"/products/$slug\"" "$TMP_DIR/products.html"; then
    echo "Hidden product $slug rendered on /products" >&2
    exit 1
  fi
  assert_404_noindex "/products/$slug" "hidden-$slug"
done

assert_404_noindex "/products/not-a-real-monitor" "unknown-product"
assert_404_noindex "/learn/not-a-real-guide" "unknown-guide"

fetch_200 /sitemap.xml "$TMP_DIR/sitemap.xml"

sitemap_product_count="$(grep -o 'https://claritybelongs.com/products/[a-z0-9-]*' "$TMP_DIR/sitemap.xml" | sort -u | wc -l | tr -d ' ')"
if [[ "$sitemap_product_count" != "${#PUBLIC_SLUGS[@]}" ]]; then
  echo "Expected ${#PUBLIC_SLUGS[@]} public product URLs in sitemap, found $sitemap_product_count" >&2
  exit 1
fi

for slug in "${PUBLIC_SLUGS[@]}"; do
  grep -q "https://claritybelongs.com/products/$slug" "$TMP_DIR/sitemap.xml"
done

for slug in "${HIDDEN_SLUGS[@]}"; do
  if grep -q "https://claritybelongs.com/products/$slug" "$TMP_DIR/sitemap.xml"; then
    echo "Hidden product $slug found in sitemap" >&2
    exit 1
  fi
done

fetch_200 / "$TMP_DIR/home.html"
if grep -Eqi '\bsmart filtering\b|\bmeaningful differences\b' "$TMP_DIR/home.html" "$TMP_DIR/products.html"; then
  echo "Unsupported semantic-monitoring wording found on public catalog surfaces" >&2
  exit 1
fi

# Every public detail page resolves through ClarityPublicCatalog. That resolver
# rejects any approved title whose adapter/config no longer matches the reviewed
# contract, so the 200 checks above also validate title-to-adapter truth.

echo "Public catalog tests passed: ${#PUBLIC_SLUGS[@]} approved products, hidden routes 404/noindex, sitemap aligned."
