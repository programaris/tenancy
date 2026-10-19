#!/usr/bin/env python3
"""Resolve synchronized PRF-compatible SemVer values without external packages."""
import argparse
import json
import re
from pathlib import Path

VERSION = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-(dev|alpha|beta|rc)\.([1-9]\d*))?$")
BASE = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")


def resolve(version_file, tag, run_number, sha):
    if not 1 <= run_number <= 65535:
        raise ValueError("run-number must be between 1 and 65535")
    if not re.fullmatch(r"[0-9a-fA-F]{40}", sha):
        raise ValueError("sha must be a full 40-character Git SHA-1")
    base = json.loads(Path(version_file).read_text(encoding="utf-8"))["nextVersion"]
    if not BASE.fullmatch(base):
        raise ValueError("nextVersion must be X.Y.Z")
    if tag:
        if not tag.startswith("v"):
            raise ValueError("tag must begin with v")
        version = tag[1:]
    else:
        version = f"{base}-dev.{run_number}"
    match = VERSION.fullmatch(version)
    if match is None:
        raise ValueError("tag must be vX.Y.Z[-alpha.N|-beta.N|-rc.N]; dev.N is CI-only")
    if tag and match.group(4) == "dev":
        raise ValueError("dev tags are not supported")
    major, minor, patch = [int(match.group(i)) for i in (1, 2, 3)]
    if any(component > 65535 for component in (major, minor, patch)):
        raise ValueError("version components must be no greater than 65535")
    if tag and (major, minor, patch) != tuple(int(x) for x in base.split(".")):
        raise ValueError("tag version must match version.json nextVersion")
    channel = match.group(4) or "stable"
    return {
        "version": version,
        "assembly_version": f"{major}.{minor}.0.0",
        "file_version": f"{major}.{minor}.{patch}.{run_number}",
        "channel": channel,
        "stable": str(channel == "stable").lower(),
        "sha_short": sha[:7].lower(),
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--version-file", default="version.json")
    parser.add_argument("--tag", default="")
    parser.add_argument("--run-number", type=int, required=True)
    parser.add_argument("--sha", required=True)
    parser.add_argument("--output", default="")
    args = parser.parse_args()
    try:
        values = resolve(args.version_file, args.tag, args.run_number, args.sha)
    except (ValueError, KeyError, json.JSONDecodeError) as exception:
        parser.error(str(exception))
    content = "".join(f"{key}={value}\n" for key, value in values.items())
    print(content, end="")
    if args.output:
        with open(args.output, "a", encoding="utf-8") as output:
            output.write(content)


if __name__ == "__main__":
    main()
