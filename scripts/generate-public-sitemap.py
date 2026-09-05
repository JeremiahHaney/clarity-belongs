#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "src" / "ClarityBelongs.Web" / "Services" / "ClarityPublicCatalog.cs"
SITEMAP = ROOT / "src" / "ClarityBelongs.Web" / "wwwroot" / "sitemap.xml"
BASE = "https://claritybelongs.com"
CORE_PATHS = [
    "/",
    "/products",
    "/learn",
    "/pricing",
    "/about",
    "/support",
    "/privacy",
    "/terms",
    "/tools",
]

source = CATALOG.read_text(encoding="utf-8")
spec_block = source.split("private static readonly PublicProductSpec[] Specs =", 1)[1].split("];", 1)[0]
slugs = re.findall(r'new\("([a-z0-9-]+)"\s*,', spec_block)

if not slugs:
    raise SystemExit("No public product slugs found in ClarityPublicCatalog Specs.")

if len(slugs) != len(set(slugs)):
    raise SystemExit("Duplicate public product slug found in ClarityPublicCatalog Specs.")

paths = CORE_PATHS + [f"/products/{slug}" for slug in slugs]
lines = [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
]
lines.extend(f"  <url><loc>{BASE}{path}</loc></url>" for path in paths)
lines.append("</urlset>")
lines.append("")
SITEMAP.write_text("\n".join(lines), encoding="utf-8")
print(f"Generated sitemap with {len(slugs)} public monitoring products.")
