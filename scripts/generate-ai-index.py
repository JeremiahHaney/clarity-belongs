#!/usr/bin/env python3
"""Deterministic, read-only source inventory. Never interprets code as verified delivery."""
import json
import re
import subprocess
from pathlib import Path
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "AI_CAPABILITIES.generated.json"

def git(*args):
    return subprocess.run(
        ["git", "-C", str(ROOT), *args],
        check=True, capture_output=True
    ).stdout

def source(path, max_bytes=160000):
    full = ROOT / path
    if not full.is_file() or full.stat().st_size > max_bytes:
        return ""
    return full.read_text(encoding="utf-8", errors="replace")

def project(path):
    refs, frameworks = [], []
    try:
        tree = ElementTree.fromstring(source(path))
        for node in tree.iter():
            tag = node.tag.rsplit("}", 1)[-1]
            if tag == "ProjectReference" and node.attrib.get("Include"):
                refs.append(node.attrib["Include"])
            if tag in ("TargetFramework", "TargetFrameworks") and node.text:
                frameworks.extend(node.text.split(";"))
    except ElementTree.ParseError:
        pass
    return {"path": path, "references": sorted(refs), "frameworks": sorted(frameworks)}

def main():
    tracked = sorted(
        x.decode("utf-8", "replace")
        for x in git("ls-files", "-z").split(b"\0")
        if x
    )
    changes = []
    for p in tracked:
        if Path(p).name.lower() != "changelog.md":
            continue
        body = source(p)
        version = re.search(
            r"(?im)^(?:#{1,3}\s+(?:version\s+)?|version\s*:\s*)(\d+\.\d+\.\d+(?:\.\d+)?)\b",
            body
        )
        changes.append({"path": p, "latest_version_in_file": version.group(1) if version else None})
    data = {
        "schema": 1,
        "warning": "Source inventory only; not proof of behavior, QA, deployment or release.",
        "projects": [
            project(p) for p in tracked
            if p.lower().endswith((".csproj", ".fsproj", ".vbproj"))
        ],
        "changelogs": changes,
        "tests": [
            p for p in tracked
            if re.search(r"(^|/)(tests?|[^/]*tests)(/|$)", p, re.I)
            and p.lower().endswith((".cs", ".fs", ".py", ".ps1", ".json"))
        ][:1200],
        "entrypoints": [
            p for p in tracked
            if Path(p).name in ("AGENTS.md", "AI_CONTEXT.md", "SYSTEM.md", "STATUS.md", "FEATURE-STATUS.md", "README.md")
        ][:500],
        "solution_files": [
            p for p in tracked if p.lower().endswith((".sln", ".slnx"))
        ],
    }
    OUTPUT.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print("Indexed", len(data["projects"]), "projects,", len(data["changelogs"]), "changelogs")

if __name__ == "__main__":
    main()
