import json
import tempfile
import unittest
from pathlib import Path

from version import resolve


class VersionTests(unittest.TestCase):
    def setUp(self):
        self._directory = tempfile.TemporaryDirectory()
        self._path = Path(self._directory.name) / "version.json"
        self._path.write_text(json.dumps({"nextVersion": "0.1.0"}), encoding="utf-8")
        self._sha = "a" * 40

    def tearDown(self):
        self._directory.cleanup()

    def test_ci_version(self):
        values = resolve(self._path, "", 14, self._sha)
        self.assertEqual("0.1.0-dev.14", values["version"])
        self.assertEqual("0.1.0.0", values["assembly_version"])
        self.assertEqual("0.1.0.14", values["file_version"])

    def test_release_tag(self):
        values = resolve(self._path, "v0.1.0", 25, self._sha)
        self.assertEqual("stable", values["channel"])
        self.assertEqual("true", values["stable"])

    def test_reject_dev_tag(self):
        with self.assertRaises(ValueError):
            resolve(self._path, "v0.1.0-dev.2", 3, self._sha)

    def test_reject_other_base_version(self):
        with self.assertRaises(ValueError):
            resolve(self._path, "v0.2.0", 3, self._sha)

    def test_reject_invalid_build_number(self):
        with self.assertRaises(ValueError):
            resolve(self._path, "", 65536, self._sha)


if __name__ == "__main__":
    unittest.main()
