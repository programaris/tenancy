# Release policy

## Version source

`version.json` declares the next planned stable version (`0.1.0`). `eng/version.py` resolves PRF-style synchronized package versions and assembly versions for the CI pipeline.

- Local development uses `0.1.0-preview.1` in `Directory.Build.props` until release approval.
- CI without a version tag produces `0.1.0-dev.N` artifacts (not published).
- An approved `v0.1.0-rc.N` tag produces release-candidate artifacts (not published).
- An approved `v0.1.0` tag produces stable build artifacts (not published by CI).

`AssemblyVersion = MAJOR.MINOR.0.0`; `FileVersion = MAJOR.MINOR.PATCH.BUILD`; `PackageVersion = MAJOR.MINOR.PATCH[-channel.N]`.

## Public publishing

This repository's CI **only validates and uploads package artifacts**. It never publishes, creates tags, or promotes releases.

Production publication requires (a) approved release commit and protected tag, (b) passing all CI/compatibility/security gates, (c) release notes, (d) version alignment and binary provenance, and (e) a separate, authorized publishing workflow using NuGet Trusted Publishing (OIDC) with environment approvals.

The internally maintained corporate PRF starter is not directly referenced from the public GitHub Actions workflow. See [PRF integration](prf-integration.md).
