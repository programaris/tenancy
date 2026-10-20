#!/usr/bin/env python3
"""Create a SHA-256 manifest for the exact files emitted by the NuGet validation build."""

import argparse
import hashlib
import json
import re
from pathlib import Path

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--directory", default="artifacts/nuget")
    parser.add_argument("--version", required=True)
    parser.add_argument("--source-sha", required=True)
    args = parser.parse_args()

    if not re.fullmatch(r"[0-9a-fA-F]{40}", args.source_sha):
        parser.error("source-sha must be a 40-character commit SHA")

    directory = Path(args.directory)

    files = sorted([*directory.glob("*.nupkg"), *directory.glob("*.snupkg")])

    if len(files) != 8:
        parser.error(f"expected eight package artifacts (found {len(files)})")
    manifest = {
        "schemaVersion": 1,
        "sourceSha": args.source_sha.lower(),
        "version": args.version,
        "artifacts": [{
            "filename": path.name,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        } for path in files],
    }

    output = directory / "release-manifest.json"

    output.write_text(json.dumps(manifest, indent=4, sort_keys=True) + "\n", encoding="utf-8")

    print(f"Wrote {output}")

if __name__ == "__main__":
    main()