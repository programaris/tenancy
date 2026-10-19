#!/usr/bin/env python3
"""Validate public NuGet package identities, metadata, dependencies and symbols."""

import argparse
import xml.etree.ElementTree as ET
import zipfile

from pathlib import Path


FRAMEWORKS = {
    "Programaris.Tenancy.Abstractions": ("netstandard2.0",),
    "Programaris.Tenancy": ("netstandard2.0", "net10.0"),
    "Programaris.Tenancy.AspNetCore": ("net10.0",),
}

ALLOWED_DEPENDENCIES = {
    "Programaris.Tenancy.Abstractions": {"NETStandard.Library"},
    "Programaris.Tenancy": {
        "Programaris.Tenancy.Abstractions",
        "NETStandard.Library",
    },
    "Programaris.Tenancy.AspNetCore": {"Programaris.Tenancy"},
}

PUBLIC_REPOSITORY = "https://github.com/programaris/tenancy"
PACKAGE_ICON = "programaris-icon.png"


def local_name(element):
    return element.tag.split("}")[-1]


def metadata_value(metadata, name):
    element = next((item for item in metadata if local_name(item) == name), None)

    return element.text if element is not None else None


def validate_packages(directory, version):
    expected = {
        f"{package_id}.{version}.{extension}"
        for package_id in FRAMEWORKS
        for extension in ("nupkg", "snupkg")
    }
    found = {
        path.name
        for extension in ("*.nupkg", "*.snupkg")
        for path in directory.glob(extension)
    }

    if found != expected:
        raise ValueError(
            f"Incorrect package artifacts; missing={sorted(expected - found)}, "
            f"unexpected={sorted(found - expected)}"
        )

    for package_id, targets in FRAMEWORKS.items():
        path = directory / f"{package_id}.{version}.nupkg"
        symbols = directory / f"{package_id}.{version}.snupkg"

        with zipfile.ZipFile(path) as package:
            names = set(package.namelist())
            nuspec = ET.fromstring(package.read(f"{package_id}.nuspec"))
            metadata = next(
                child for child in nuspec if local_name(child) == "metadata"
            )

            if metadata_value(metadata, "id") != package_id:
                raise ValueError(f"Invalid NuGet package ID: {package_id}")

            if metadata_value(metadata, "version") != version:
                raise ValueError(f"Invalid package version: {package_id}")

            if metadata_value(metadata, "authors") != "PROGRAMARIS":
                raise ValueError(f"Invalid package authors: {package_id}")

            if not metadata_value(metadata, "description"):
                raise ValueError(f"Missing package description: {package_id}")

            if metadata_value(metadata, "readme") != "README.md" or "README.md" not in names:
                raise ValueError(f"Missing package README: {package_id}")

            license_element = next(
                (child for child in metadata if local_name(child) == "license"),
                None,
            )

            if (
                license_element is None
                or license_element.text != "Apache-2.0"
                or license_element.attrib.get("type") != "expression"
            ):
                raise ValueError(f"Invalid package license: {package_id}")

            if metadata_value(metadata, "icon") != PACKAGE_ICON or PACKAGE_ICON not in names:
                raise ValueError(f"Missing package icon: {package_id}")

            if package.read(PACKAGE_ICON)[:8] != b"\x89PNG\r\n\x1a\n":
                raise ValueError(f"Invalid PNG icon: {package_id}")

            repository = next(
                (child for child in metadata if local_name(child) == "repository"),
                None,
            )

            if (
                repository is None
                or repository.attrib.get("type") != "git"
                or repository.attrib.get("url") != PUBLIC_REPOSITORY
            ):
                raise ValueError(f"Invalid repository provenance metadata: {package_id}")

            allowed = ALLOWED_DEPENDENCIES[package_id]

            for dependency in metadata.iter():
                if local_name(dependency) != "dependency":
                    continue

                dependency_id = dependency.attrib.get("id", "")
                dependency_version = dependency.attrib.get("version", "")

                if dependency_id not in allowed:
                    raise ValueError(
                        f"Unexpected dependency {dependency_id}: {package_id}"
                    )

                if dependency_id.startswith("Programaris.Tenancy") and version not in dependency_version:
                    raise ValueError(
                        f"Dependency version mismatch for {dependency_id}: {package_id}"
                    )

            for framework in targets:
                if f"lib/{framework}/{package_id}.dll" not in names:
                    raise ValueError(f"Missing target {framework}: {package_id}")

        with zipfile.ZipFile(symbols) as symbol_package:
            symbol_names = set(symbol_package.namelist())

            for framework in targets:
                if f"lib/{framework}/{package_id}.pdb" not in symbol_names:
                    raise ValueError(
                        f"Missing portable symbols for {framework}: {package_id}"
                    )

        print(f"Verified {package_id} {version}: {', '.join(targets)}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--directory", default="artifacts/nuget")
    parser.add_argument("--version", required=True)
    args = parser.parse_args()

    try:
        validate_packages(Path(args.directory), args.version)
    except (ValueError, KeyError, ET.ParseError, zipfile.BadZipFile) as exception:
        parser.error(str(exception))


if __name__ == "__main__":
    main()
