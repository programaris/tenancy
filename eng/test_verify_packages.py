import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile

from pathlib import Path

from verify_packages import FRAMEWORKS, validate_packages


class PackageValidationTests(unittest.TestCase):
    VERSION = "0.1.0-preview.1"
    PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"

    def setUp(self):
        self._TemporaryDirectory = tempfile.TemporaryDirectory()
        self._Directory = Path(self._TemporaryDirectory.name)

        for package_id in FRAMEWORKS:
            self._WritePackage(package_id)

    def tearDown(self):
        self._TemporaryDirectory.cleanup()

    def _WritePackage(
        self,
        package_id,
        *,
        dependency_id=None,
        dependency_version=None,
        corrupt_icon=False,
        omit_symbol=False,
    ):
        root = ET.Element("package")
        metadata = ET.SubElement(root, "metadata")

        for name, value in (
            ("id", package_id),
            ("version", self.VERSION),
            ("authors", "PROGRAMARIS"),
            ("description", "Fixture for package certification"),
            ("readme", "README.md"),
            ("icon", "programaris-icon.png"),
        ):
            ET.SubElement(metadata, name).text = value

        ET.SubElement(metadata, "license", {"type": "expression"}).text = "Apache-2.0"
        ET.SubElement(
            metadata,
            "repository",
            {"type": "git", "url": "https://github.com/programaris/tenancy"},
        )

        if dependency_id:
            dependencies = ET.SubElement(metadata, "dependencies")
            group = ET.SubElement(dependencies, "group")
            ET.SubElement(
                group,
                "dependency",
                {"id": dependency_id, "version": dependency_version or self.VERSION},
            )

        package_path = self._Directory / f"{package_id}.{self.VERSION}.nupkg"

        with zipfile.ZipFile(package_path, "w") as package:
            package.writestr(f"{package_id}.nuspec", ET.tostring(root))
            package.writestr("README.md", "PROGRAMARIS Tenancy")
            package.writestr(
                "programaris-icon.png",
                b"invalid" if corrupt_icon else self.PNG_SIGNATURE,
            )

            for framework in FRAMEWORKS[package_id]:
                package.writestr(f"lib/{framework}/{package_id}.dll", b"fixture")

        symbol_path = self._Directory / f"{package_id}.{self.VERSION}.snupkg"

        with zipfile.ZipFile(symbol_path, "w") as symbols:
            if not omit_symbol:
                for framework in FRAMEWORKS[package_id]:
                    symbols.writestr(f"lib/{framework}/{package_id}.pdb", b"fixture")

    def test_accepts_valid_package_inventory(self):
        validate_packages(self._Directory, self.VERSION)

    def test_rejects_unexpected_package(self):
        (self._Directory / "unexpected.0.1.0.nupkg").write_bytes(b"invalid")

        with self.assertRaisesRegex(ValueError, "unexpected"):
            validate_packages(self._Directory, self.VERSION)

    def test_rejects_missing_portable_symbols(self):
        self._WritePackage("Programaris.Tenancy", omit_symbol=True)

        with self.assertRaisesRegex(ValueError, "Missing portable symbols"):
            validate_packages(self._Directory, self.VERSION)

    def test_rejects_private_dependencies(self):
        self._WritePackage(
            "Programaris.Tenancy",
            dependency_id="Programaris.Identity.Internal",
        )

        with self.assertRaisesRegex(ValueError, "Unexpected dependency"):
            validate_packages(self._Directory, self.VERSION)

    def test_rejects_unsynchronized_package_dependency(self):
        self._WritePackage(
            "Programaris.Tenancy.AspNetCore",
            dependency_id="Programaris.Tenancy",
            dependency_version="9.9.9",
        )

        with self.assertRaisesRegex(ValueError, "Dependency version mismatch"):
            validate_packages(self._Directory, self.VERSION)

    def test_rejects_corrupt_icon(self):
        self._WritePackage("Programaris.Tenancy", corrupt_icon=True)

        with self.assertRaisesRegex(ValueError, "Invalid PNG icon"):
            validate_packages(self._Directory, self.VERSION)


if __name__ == "__main__":
    unittest.main()
