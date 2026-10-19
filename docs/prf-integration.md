# Programaris Release Flow integration — public-safe preparation

This public repository implements the **compatible version and NuGet package inventory contract**, not the complete private PRF release automations.

- `version.json` contains the stable version line.
- `eng/version.py` resolves `version`, `assembly_version`, `file_version`, `channel`, and SHA metadata, following our synchronized semantic-version conventions.
- `distribution.json` uses the PRF schema-1 package inventory with exactly three public NuGet packages.
- `eng/validate_distribution.py` checks the inventory and project metadata.
- `eng/verify_packages.py` examines generated packages, target frameworks, license and README.
- `eng/smoke_consumer.py` runs isolated .NET 8 portable and .NET 10 ASP.NET Core consumers against generated NuGet packages.
- `eng/create_artifact_manifest.py` records SHA-256 hashes and the exact source commit.
- `.github/workflows/ci.yml` compiles and tests on GitHub-hosted Ubuntu/Windows runners, packs artifacts on Ubuntu, and **never publishes**.

## Release gating

No workflow in this repository may call a private PRF checkout from untrusted PRs or execute forked code on a persistent self-hosted runner. A future separate publishing workflow must use protected environments, immutable release tags and OIDC Trusted Publishing to nuget.org. Establish that workflow only after the CI and end-to-end consumer smoke test succeed.

The private PRF reference currently treats a complete multi-package publisher as an incremental capability. Do not describe this repository's preliminary CI as full PRF adoption or claim that `v0.1.0` has been released until verified publishing and package installation are complete.
