#!/usr/bin/env python3
"""Evidence-only AI repository index. Standard-library Python, deterministic output."""
import argparse
import json
import re
import subprocess
import sys
from pathlib import Path
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parent.parent
JSON_FILE = ROOT / "AI_CAPABILITIES.generated.json"
TEXT_FILE = ROOT / "AI_INDEX.generated.md"
EXCLUDE = {".patch-backup", ".patch-backups", "node_modules", "bin", "obj",
           "packages", "artifacts", ".venv", "venv", ".vs", ".idea",
           "coverage", "TestResults", "dist", "build", ".next", ".cache"}
DOC_NAMES = {"AGENTS.md", "AI_CONTEXT.md", "SYSTEM.md", "README.md",
             "STATUS.md", "FEATURE-STATUS.md", "PRODUCT-MATURITY-REVIEWS.md"}
ROUTE = re.compile(r'\b(?:MapGet|MapPost|MapPut|MapDelete|MapPatch|MapGroup|MapMethods|'
                   r'Route|HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)\s*\(\s*"([^"\\\n]{1,140})"')
TYPE = re.compile(r'\b(?:public\s+|internal\s+)?(?:sealed\s+|abstract\s+|static\s+|partial\s+)*'
                  r'(?:class|interface|record)\s+(\w{3,90})\b')
VERSION = re.compile(r'(?im)^(?:#{1,3}\s+(?:version\s+)?|version\s*:\s*)'
                     r'(\d+\.\d+\.\d+(?:\.\d+)?)\b')

def tracked_files():
    raw = subprocess.run(["git", "-C", str(ROOT), "ls-files", "-z"],
                         check=True, stdout=subprocess.PIPE).stdout
    return sorted(p.decode("utf-8", "replace") for p in raw.split(b"\0")
                  if p and not any(part in EXCLUDE for part in Path(p.decode("utf-8", "replace")).parts))

def read_file(path):
    full = ROOT / path
    if not full.is_file() or full.is_symlink() or full.stat().st_size > 250000:
        return ""
    return full.read_text(encoding="utf-8", errors="replace")

def project(path):
    entry = {"path": path, "frameworks": [], "references": [], "packages": []}
    try:
        for item in ElementTree.fromstring(read_file(path)).iter():
            tag = item.tag.rsplit("}", 1)[-1]
            if tag in ("TargetFramework", "TargetFrameworks") and item.text:
                entry["frameworks"].extend(item.text.split(";"))
            elif tag == "ProjectReference" and item.get("Include"):
                entry["references"].append(item.get("Include").replace("\\", "/"))
            elif tag == "PackageReference" and item.get("Include"):
                entry["packages"].append(item.get("Include"))
    except ElementTree.ParseError:
        entry["parse_warning"] = "Invalid project XML"
    for key in ("frameworks", "references", "packages"):
        entry[key] = sorted(set(entry[key]))
    return entry

def build():
    files = tracked_files()
    projects = [p for p in files if p.lower().endswith((".csproj", ".fsproj", ".vbproj"))]
    changes = [p for p in files if Path(p).name.lower() == "changelog.md"]
    tests = [p for p in files if
             (re.search(r"(^|/)(?:tests?|[^/]*tests)(/|$)", p, re.I)
              or Path(p).name.endswith("Tests.cs"))
             and Path(p).suffix.lower() in (".cs", ".fs", ".py", ".ps1", ".csproj", ".json")]
    routes, symbols = [], []
    for path in files:
        if Path(path).suffix.lower() not in (".cs", ".razor", ".cshtml"):
            continue
        if len(routes) >= 350 and len(symbols) >= 350:
            break
        for line_no, line in enumerate(read_file(path).splitlines(), 1):
            if len(routes) < 350:
                for match in ROUTE.finditer(line):
                    if match.group(1).startswith(("/", "{")):
                        routes.append({"path": path, "line": line_no, "pattern": match.group(1)})
            if len(symbols) < 350 and any(x in line for x in ("class ", "interface ", "record ")):
                match = TYPE.search(line)
                if match:
                    symbols.append({"path": path, "line": line_no, "symbol": match.group(1)})
    return {
        "schema": 2,
        "evidence_only": True,
        "warning": "Declarations are navigation evidence, not evidence of QA, correct behavior, deployment, or release.",
        "projects": [project(p) for p in projects[:500]],
        "routes": routes,
        "type_declarations": symbols,
        "tests": tests[:700],
        "changelogs": [
            {"path": p, "first_version_in_file": m.group(1) if (m := VERSION.search(read_file(p))) else None}
            for p in changes[:500]
        ],
        "entry_documents": [p for p in files if Path(p).name in DOC_NAMES][:500],
        "solutions": [p for p in files if p.lower().endswith((".sln", ".slnx"))][:500],
        "status_sources": [p for p in files if Path(p).name in ("STATUS.md", "FEATURE-STATUS.md")][:500],
        "counts": {"projects": len(projects), "routes_indexed": len(routes),
                   "tests_indexed": min(len(tests), 700), "changelogs": len(changes)}
    }

def summary(data):
    c = data["counts"]
    lines = [
        "# AI Source Navigation (automatically generated)", "",
        "> Source evidence, not a verified feature/release ledger. Read `AGENTS.md` and `AI_CONTEXT.md` first.",
        "> Do not manually edit. More details and line-level declarations: `AI_CAPABILITIES.generated.json`.", "",
        "## Coverage", "",
        f"- Projects: {c['projects']}; route declarations: {c['routes_indexed']}; tests: {c['tests_indexed']}; changelogs: {c['changelogs']}.",
        "- Backup and build folders are excluded. Entries are bounded to keep AI context compact.", "",
        "## Where to begin", ""
    ]
    lines += ["- `" + p + "`" for p in data["entry_documents"][:35]]
    lines += ["", "## Solutions", ""]
    lines += ["- `" + p + "`" for p in data["solutions"][:30]]
    lines += ["", "## Project map", ""]
    lines += ["- `" + p["path"] + "` (" + ", ".join(p["frameworks"]) + ")"
              for p in data["projects"][:50]]
    lines += ["", "## First changelog version per file", ""]
    lines += ["- `" + p["path"] + "`: " + (p["first_version_in_file"] or "unrecognized")
              for p in data["changelogs"][:50]]
    lines += ["", "## Agent procedure", "",
              "1. Identify ownership via portfolio Atlas and this repository's context.",
              "2. Search the JSON for existing route/type/project/test evidence; inspect actual source.",
              "3. Read owning feature status and latest project changelog before proposing changes.",
              "4. Do not confuse a declaration, integration boundary or test filename with verified behavior.",
              "5. Prefer improving, connecting, testing or publishing existing capabilities.", ""]
    return "\n".join(lines)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="Validate generated output without changes")
    args = parser.parse_args()
    data = build()
    outputs = [(JSON_FILE, json.dumps(data, indent=2, ensure_ascii=False) + "\n"),
               (TEXT_FILE, summary(data))]
    if args.check:
        stale = [p.name for p, s in outputs if not p.is_file() or p.read_text(encoding="utf-8") != s]
        if stale:
            print("Stale generated AI context:", ", ".join(stale), file=sys.stderr)
            return 1
        print("AI index verified")
        return 0
    for p, content in outputs:
        p.write_text(content, encoding="utf-8")
    print("AI index generated:", data["counts"])
    return 0

if __name__ == "__main__":
    sys.exit(main())
