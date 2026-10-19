#!/usr/bin/env python3
"""Check public package identifiers, TFMs and metadata without publishing."""
import argparse
import json
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

FRAMEWORKS = {
    "Programaris.Tenancy.Abstractions": ("netstandard2.0",),
    "Programaris.Tenancy": ("netstandard2.0", "net10.0"),
    "Programaris.Tenancy.AspNetCore": ("net10.0",),
}


def text(metadata, name):
    child = next((value for value in metadata if value.tag.split("}")[-1] == name), None)
    return child.text if child is not None else None


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--directory", default="artifacts/nuget")
    parser.add_argument("--version", required=True)
    args = parser.parse_args()
    output = Path(args.directory)
    for package_id, targets in FRAMEWORKS.items():
        path = output / f"{package_id}.{args.version}.nupkg"
        symbols = output / f"{package_id}.{args.version}.snupkg"
        if not path.is_file() or not symbols.is_file():
            parser.error(f"Missing package or symbols for {package_id}")
        with zipfile.ZipFile(path) as package:
            names = set(package.namelist())
            nuspec = ET.fromstring(package.read(f"{package_id}.nuspec"))
            metadata = next(c for c in nuspec if c.tag.split("}")[-1] == "metadata")
            if text(metadata, "id") != package_id or text(metadata, "version") != args.version:
                parser.error(f"Incorrect package identity: {package_id}")
            if text(metadata, "readme") != "README.md" or "README.md" not in names:
                parser.error(f"Missing README in package: {package_id}")
            license_node = next((c for c in metadata if c.tag.split("}")[-1] == "license"), None)
            if license_node is None or license_node.text != "Apache-2.0":
                parser.error(f"Incorrect license metadata: {package_id}")
            for framework in targets:
                if f"lib/{framework}/{package_id}.dll" not in names:
                    parser.error(f"Missing target {framework}: {package_id}")
        print(f"Verified {package_id} {args.version}: {', '.join(targets)}")


if __name__ == "__main__":
    main()
