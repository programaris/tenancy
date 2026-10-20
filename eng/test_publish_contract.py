"""Static release-pipeline regression tests. No third-party YAML parser required."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IDS = ("Programaris.Tenancy.Abstractions", "Programaris.Tenancy", "Programaris.Tenancy.AspNetCore", "Programaris.Tenancy.Http")

class PublishContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = (ROOT / ".github/workflows/publish.yml").read_text(encoding="utf-8")

    def test_version_and_source_pinning(self):
        self.assertEqual("0.2.0", json.loads((ROOT / "version.json").read_text(encoding="utf-8"))["nextVersion"])
        self.assertIn("options: [v0.2.0]", self.workflow)
        self.assertIn("inputs.release_tag == 'v0.2.0'", self.workflow)
        self.assertIn('test "$SHA" = "$TAG_SHA"', self.workflow)
        self.assertIn('test "$SHA" = "$(git rev-parse origin/dev)"', self.workflow)
        self.assertNotIn("v0.1.0", self.workflow)

    def test_exactly_four_packages_and_manifest(self):
        self.assertIn("for id in " + " ".join(IDS) + "; do", self.workflow)
        self.assertIn("for id in " + " ".join(x.lower() for x in IDS) + "; do", self.workflow)
        self.assertIn("tenancy-nuget-0.2.0", self.workflow)
        self.assertIn("eng/verify_packages.py --version 0.2.0", self.workflow)
        self.assertIn("eng/verify_artifact_manifest.py", self.workflow)
        self.assertIn("SourceLinkProbe/SourceLinkProbe.csproj", self.workflow)
        self.assertNotIn("--skip-duplicate", self.workflow)

    def test_manual_protected_oidc_promotion(self):
        self.assertIn("workflow_dispatch:", self.workflow)
        self.assertIn("environment: nuget-production", self.workflow)
        self.assertIn("id-token: write", self.workflow)
        self.assertIn("uses: NuGet/login@v1", self.workflow)
        self.assertIn("inputs.confirmation == 'PUBLISH'", self.workflow)
        self.assertIn("--notes-file docs/releases/v0.2.0-notes.md", self.workflow)
        self.assertNotIn("pull_request:", self.workflow)

if __name__ == "__main__":
    unittest.main()
