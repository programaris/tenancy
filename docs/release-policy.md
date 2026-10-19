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

Production publication requires (a) approved `v0.1.0` tag pointing to protected `dev` HEAD, (b) passing Windows/Linux tagged CI, portable consumer, SourceLink and SHA-256 integrity gates, (c) release notes, (d) the protected `nuget-production` environment, and (e) explicit manual execution of `.github/workflows/publish.yml` with `PUBLISH` confirmation. That workflow downloads **the exact tagged CI packages**, verifies them independently, and uses NuGet Trusted Publishing OIDC without a long-lived API key. Successful publication is followed by a GitHub Release with six artifacts and a manifest. `v0.1.0-rc.1` is for certification only and is never published.

The internally maintained corporate PRF starter is not directly referenced from the public GitHub Actions workflow. See [PRF integration](prf-integration.md).
