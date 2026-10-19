import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from verify_artifact_manifest import PACKAGES, verify_manifest


class ArtifactManifestTests(unittest.TestCase):
    VERSION = "0.1.0"
    SHA = "a" * 40

    def setUp(self):
        self._TemporaryDirectory = tempfile.TemporaryDirectory()
        self._Directory = Path(self._TemporaryDirectory.name)
        entries = []

        for name in PACKAGES:
            for extension in ("nupkg", "snupkg"):
                filename = f"{name}.{self.VERSION}.{extension}"
                content = filename.encode("utf-8")
                (self._Directory / filename).write_bytes(content)
                entries.append({
                    "filename": filename,
                    "sha256": hashlib.sha256(content).hexdigest(),
                })

        self._Manifest = {
            "schemaVersion": 1,
            "version": self.VERSION,
            "sourceSha": self.SHA,
            "artifacts": entries,
        }
        self._Write()

    def tearDown(self):
        self._TemporaryDirectory.cleanup()

    def _Write(self):
        (self._Directory / "release-manifest.json").write_text(
            json.dumps(self._Manifest), encoding="utf-8"
        )

    def test_valid_manifest(self):
        verify_manifest(self._Directory, self.VERSION, self.SHA)

    def test_wrong_source_sha(self):
        with self.assertRaisesRegex(ValueError, "SHA mismatch"):
            verify_manifest(self._Directory, self.VERSION, "f" * 40)

    def test_wrong_version(self):
        with self.assertRaisesRegex(ValueError, "version mismatch"):
            verify_manifest(self._Directory, "0.2.0", self.SHA)

    def test_corrupt_package(self):
        (self._Directory / self._Manifest["artifacts"][0]["filename"]).write_bytes(b"changed")

        with self.assertRaisesRegex(ValueError, "SHA-256 mismatch"):
            verify_manifest(self._Directory, self.VERSION, self.SHA)

    def test_duplicate_entries(self):
        self._Manifest["artifacts"][1]["filename"] = self._Manifest["artifacts"][0]["filename"]
        self._Write()

        with self.assertRaisesRegex(ValueError, "duplicate"):
            verify_manifest(self._Directory, self.VERSION, self.SHA)

    def test_missing_package(self):
        (self._Directory / self._Manifest["artifacts"][0]["filename"]).unlink()

        with self.assertRaisesRegex(ValueError, "inventory"):
            verify_manifest(self._Directory, self.VERSION, self.SHA)


if __name__ == "__main__":
    unittest.main()
