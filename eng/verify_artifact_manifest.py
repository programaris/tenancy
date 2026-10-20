#!/usr/bin/env python3
"""Validate eight immutable NuGet artifacts against their SHA-256 manifest."""

import argparse
import hashlib
import json
import re
from pathlib import Path

PACKAGES = ("Programaris.Tenancy.Abstractions", "Programaris.Tenancy", "Programaris.Tenancy.AspNetCore", "Programaris.Tenancy.Http")


def verify_manifest(directory, version, source_sha):
    manifest = json.loads((directory / "release-manifest.json").read_text(encoding="utf-8"))

    if manifest.get("schemaVersion") != 1 or manifest.get("version") != version:
        raise ValueError("Release manifest schema or version mismatch")

    if manifest.get("sourceSha") != source_sha.lower():
        raise ValueError("Release manifest source SHA mismatch")

    expected = {f"{name}.{version}.{extension}" for name in PACKAGES for extension in ("nupkg", "snupkg")}
    actual = {path.name for pattern in ("*.nupkg", "*.snupkg") for path in directory.glob(pattern)}
    entries = manifest.get("artifacts")

    if actual != expected or not isinstance(entries, list) or len(entries) != 8:
        raise ValueError("Invalid release package inventory")

    recorded = [item.get("filename") for item in entries]

    if set(recorded) != expected or len(set(recorded)) != 8:
        raise ValueError("Invalid or duplicate manifest filenames")

    for entry in entries:
        name = entry["filename"]
        digest = entry.get("sha256")

        if not isinstance(digest, str) or not re.fullmatch(r"[0-9a-fA-F]{64}", digest):
            raise ValueError(f"Invalid SHA-256 digest: {name}")

        observed = hashlib.sha256((directory / name).read_bytes()).hexdigest()

        if observed != digest.lower():
            raise ValueError(f"SHA-256 mismatch: {name}")

    print(f"Eight NuGet artifacts verified for {version} ({source_sha})")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--directory", default="artifacts/nuget")
    parser.add_argument("--version", required=True)
    parser.add_argument("--source-sha", required=True)
    args = parser.parse_args()

    if not re.fullmatch(r"[0-9a-fA-F]{40}", args.source_sha):
        parser.error("Full 40-character source SHA required")

    try:
        verify_manifest(Path(args.directory), args.version, args.source_sha)
    except (FileNotFoundError, KeyError, ValueError, json.JSONDecodeError) as exception:
        parser.error(str(exception))


if __name__ == "__main__":
    main()
