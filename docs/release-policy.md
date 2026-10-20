# Release policy

## Versions

The source of truth is `version.json` (`nextVersion: 0.2.0`). `eng/version.py` resolves synchronized four-package SemVer, assembly versions and file versions. Local development uses `0.2.0-preview.1` and untagged CI builds `0.2.0-dev.N`. Tagged `v0.2.0-rc.N` builds are certification-only; they are never published to NuGet.org.

`AssemblyVersion = MAJOR.MINOR.0.0`; `FileVersion = MAJOR.MINOR.PATCH.BUILD`; `PackageVersion = MAJOR.MINOR.PATCH[-channel.N]`.

## v0.2.0 release gates

1. Certify Windows/Linux Release build/test and exact SourceLink/dependency/SHA-256 manifest for all four `.nupkg` and four `.snupkg`, plus isolated .NET 8/.NET 10 consumers.
2. Review v0.1.0 compatibility, security, tenant selection/validation/authorization and fail-closed scenarios. Obtain independent Identity/Gateway contract-integration evidence against a v0.2.0 candidate (a host must supply its own trusted adapters).
3. Approve a source-pinned `v0.2.0-rc.1` and successful tag CI. A change to approved source requires a new candidate and re-certification.
4. Obtain explicit production approval; create `v0.2.0` pointing to the exact protected `dev` HEAD. Confirm successful stable tag CI and verify all eight immutable files.
5. Manually dispatch `.github/workflows/publish.yml` from `dev` with tag `v0.2.0`, confirmation `PUBLISH`, approved protected environment `nuget-production`, and short-lived NuGet Trusted Publishing credentials. The workflow publishes exactly four packages, checks NuGet.org availability, and creates a GitHub Release.

No automatic publication or embedded NuGet API key is authorized. Never retag/rebuild immutable published versions or silently skip duplicate package versions. See [v0.2.0 runbook](operations/nuget-v0.2.0-publishing.md). The already-published v0.1.0 remains documented in its [historical runbook](operations/nuget-v0.1.0-publishing.md).
