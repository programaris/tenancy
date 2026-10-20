#!/usr/bin/env python3
"""Verify that the public NuGet package inventory points to real packable projects."""
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def main():
    root = Path(__file__).resolve().parents[1]
    manifest = json.loads((root / "distribution.json").read_text(encoding="utf-8"))
    if manifest.get("schemaVersion") != 1 or manifest.get("versionPolicy") != "synchronized":
        raise ValueError("Unsupported distribution manifest")
    packages = manifest.get("packages", [])
    if len(packages) != 4:
        raise ValueError("Expected four public NuGet packages")
    package_ids = set()
    for package in packages:
        if package.get("kind") != "nuget":
            raise ValueError("Only NuGet packages are available in v0.2.0")
        path = root / package["path"]
        if not path.resolve().is_relative_to(root.resolve()) or not path.is_file():
            raise ValueError(f"Missing or external project: {path}")
        project = ET.parse(path).getroot()
        project_id = project.findtext("./PropertyGroup/PackageId")
        if project_id != package["id"] or project_id in package_ids:
            raise ValueError(f"Invalid or duplicated package id: {project_id}")
        if project.findtext("./PropertyGroup/IsPackable") != "true":
            raise ValueError(f"Project is not packable: {path}")
        package_ids.add(project_id)
    if manifest.get("dependencies") != []:
        raise ValueError("Cross-repository dependencies are not part of this release")
    print(f"Validated {len(package_ids)} public NuGet packages")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, KeyError, OSError, ET.ParseError, json.JSONDecodeError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
