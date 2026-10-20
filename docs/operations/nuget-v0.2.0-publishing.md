# PROGRAMARIS Tenancy v0.2.0 — NuGet publication

## Four packages

- `Programaris.Tenancy.Abstractions`
- `Programaris.Tenancy`
- `Programaris.Tenancy.AspNetCore`
- `Programaris.Tenancy.Http` (new package)

Each produces an immutable `.nupkg` and `.snupkg` at identical version `0.2.0`.

## Certification (required before publication)

1. Keep final source on protected `dev`; finish independent Identity adapter integration using a pinned candidate build. Do not publish unverified `-dev` packages to production.
2. Tag the **exact** intended `dev` HEAD as `v0.2.0-rc.1`. Tag CI must pass Windows/Linux, .NET 8/.NET 10 isolated consumers, package metadata, SourceLink and the eight-file SHA-256 manifest.
3. Review compatibility to v0.1.0 and security scenarios: no authorization from a header, unknown/inactive tenant, anonymous rejection before protected registry lookup, cross-tenant/operation denial, outage fail-closed, OIDC selection-source trust, context isolation and permission revocation.
4. Approve RC evidence and release SHA. If source changes, create a new RC and repeat the certification.

## Promotion

1. Verify NuGet.org Trusted Publishing policy for GitHub owner `programaris`, repo `tenancy`, workflow `publish.yml`, protected environment `nuget-production`, and four IDs including new `Programaris.Tenancy.Http` (new package creation must be permitted). Configure required environment approval; no long-lived API keys.
2. Tag exact approved protected `dev` HEAD as `v0.2.0`. Wait for successful tag-triggered CI and artifact `tenancy-nuget-0.2.0`.
3. In GitHub Actions, run **Publish NuGet v0.2.0** from `dev`; choose `v0.2.0`, provide the NuGet.org account username and type `PUBLISH`.
4. Approve `nuget-production`. The workflow checks tag = `dev` HEAD, downloads only the certified tag artifacts, independently verifies eight hashes and SourceLink, exchanges OIDC credentials, publishes the four packages/symbols, checks NuGet.org feed versions and creates a GitHub Release with its manifest.
5. Confirm new isolated consumer installations resolve all four public packages directly from NuGet.org.

## Partial failure

NuGet versions are immutable. **Do not** rerun the full workflow blindly, retag, rebuild, or conceal duplicates. Verify which exact packages are published, compare against the manifest and plan a separate controlled recovery for missing packages only, using original certified artifacts. A failed OIDC exchange may indicate incorrect Trusted Publishing owner/repo/workflow/environment/package policy.
