#!/usr/bin/env python3
"""Send only sanitized Gitleaks observations to the private APIT ingestion API."""
import hashlib
import json
import os
import pathlib
import sys
import urllib.error
import urllib.request

source = pathlib.Path(sys.argv[1])
raw = json.loads(source.read_text(encoding="utf-8"))
if not isinstance(raw, list):
    raise SystemExit("Expected a Gitleaks JSON array")

rows = []
for item in raw:
    if not isinstance(item, dict):
        continue
    # Never copy Secret, Match, Line, Description, or raw snippets to the API.
    name = pathlib.PurePosixPath(str(item.get("File", "")).replace("\\", "/")).name[:260]
    rule = str(item.get("RuleID", ""))[:100]
    commit = str(item.get("Commit", ""))[:64]
    line = item.get("StartLine")
    if not name or not rule:
        continue
    line = line if isinstance(line, int) and line > 0 else None
    identity = "\0".join((os.environ["GITHUB_REPOSITORY"], rule, str(item.get("File", "")), str(line), commit))
    rows.append({
        "rule": rule,
        "file": name,
        "line": line,
        "commit": commit or None,
        "fingerprint": hashlib.sha256(identity.encode()).hexdigest(),
    })

key = os.environ.get("APIT_SECURITY_INGEST_KEY", "")
if len(key) < 32:
    raise SystemExit("APIT_SECURITY_INGEST_KEY is missing or too short")

# Large historical scans contain many findings; each chunk has a unique run key.
# No empty scan run should silently imply all historical findings are resolved.
chunks = [rows[index:index + 1000] for index in range(0, len(rows), 1000)] or [[]]
for part, findings in enumerate(chunks, 1):
    payload = json.dumps({
        "assetKey": "github:" + os.environ["GITHUB_REPOSITORY"],
        "scanner": "Gitleaks",
        "runKey": os.environ["GITHUB_RUN_ID"] + ":" + os.environ.get("GITHUB_RUN_ATTEMPT", "1") + ":" + str(part),
        "revision": os.environ.get("GITHUB_SHA", ""),
        "startedUtc": __import__("datetime").datetime.now(__import__("datetime").timezone.utc).isoformat(),
        "result": "completed",
        "findings": findings
    }).encode()

    request = urllib.request.Request(
        "https://autopilotit.net/api/owner/security/ingest/gitleaks",
        data=payload,
        headers={
            "Content-Type": "application/json",
            "X-APIT-Security-Ingest-Key": key,
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=40) as response:
            print("Sanitized Gitleaks ingestion:", response.status, len(findings), "observations")
    except urllib.error.HTTPError as error:
        print("Security ingestion failed: HTTP", error.code, file=sys.stderr)
        raise SystemExit(1)
