#!/usr/bin/env python3
"""Regression tests for deterministic AI index extraction. No external packages."""
import importlib.util
import json
import subprocess
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).with_name("generate-ai-index.py")
spec = importlib.util.spec_from_file_location("ai_index_generator", SCRIPT)
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)

class IndexTests(unittest.TestCase):
    def test_source_discovery_and_backup_exclusion(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            subprocess.run(["git", "init", "-q", str(root)], check=True)
            for folder in ("src", "Tests", ".patch-backup"):
                (root / folder).mkdir()
            (root / "src" / "Application.csproj").write_text(
                "<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>",
                encoding="utf-8"
            )
            (root / "src" / "Program.cs").write_text(
                'app.MapGet("/health", () => "ok");\npublic class HealthService {}\n',
                encoding="utf-8"
            )
            (root / "Tests" / "HealthServiceTests.cs").write_text(
                "public class HealthServiceTests {}\n", encoding="utf-8"
            )
            (root / ".patch-backup" / "Old.csproj").write_text(
                "<Project />", encoding="utf-8"
            )
            (root / "src" / "CHANGELOG.md").write_text(
                "## 2.3.4 - current\n## 2.3.3 - prior\n", encoding="utf-8"
            )
            subprocess.run(["git", "-C", str(root), "add", "."], check=True)
            original = generator.ROOT
            try:
                generator.ROOT = root
                first = generator.build()
                second = generator.build()
            finally:
                generator.ROOT = original
            self.assertEqual(json.dumps(first, sort_keys=True), json.dumps(second, sort_keys=True))
            self.assertEqual(first["schema"], 2)
            self.assertEqual(first["counts"]["projects"], 1)
            self.assertTrue(any(route["pattern"] == "/health" for route in first["routes"]))
            self.assertTrue(any(item["symbol"] == "HealthService" for item in first["type_declarations"]))
            self.assertEqual(first["changelogs"][0]["first_version_in_file"], "2.3.4")
            self.assertEqual(first["projects"][0]["frameworks"], ["net10.0"])
            self.assertFalse(any(".patch-backup" in x["path"] for x in first["projects"]))
            self.assertTrue(any("Tests/HealthServiceTests.cs" == x for x in first["tests"]))

if __name__ == "__main__":
    unittest.main()
