# Release checklist — v0.1.0

> **Certification in progress — not approved for publication.** Checked gates reference the development CI baseline `3b76ab7` (2026-10-19), not final tagged binaries. See the [evidence register](releases/v0.1.0.md). Relevant checks must be repeated against the approved release candidate.
>
> Each gate must be evidenced by CI, an isolated consumer test, or documented review.

## Public API and behavior

- [ ] Verify stable public contracts for `TenantContext`, `ITenantContextAccessor` and `TenantSlug`. ([E07](releases/v0.1.0.md#e07))
- [x] Confirm middleware fail-closed behavior and explicit endpoint exceptions. ([E02](releases/v0.1.0.md#e02))
- [x] Confirm no default tenant or global opt-out exists; missing tenant remains absent on optional routes. ([E02](releases/v0.1.0.md#e02))
- [x] Confirm malformed, repeated and conflicting tenant headers fail. ([E02](releases/v0.1.0.md#e02))
- [x] Confirm authorization remains the consuming application's responsibility. ([E03](releases/v0.1.0.md#e03))
- [x] Confirm propagation is opt-in and rejects pre-existing tenant headers. ([E02](releases/v0.1.0.md#e02))

## Compatibility and quality

- [x] `dotnet restore Programaris.Tenancy.slnx` succeeds. ([E01](releases/v0.1.0.md#e01))
- [x] `dotnet build Programaris.Tenancy.slnx -c Release -warnaserror` succeeds. ([E01](releases/v0.1.0.md#e01))
- [x] `dotnet test Programaris.Tenancy.slnx -c Release` succeeds on Windows and Linux. ([E01](releases/v0.1.0.md#e01))
- [x] Concurrent request tests complete successfully, with no cross-tenant leakage. ([E02](releases/v0.1.0.md#e02))
- [x] NuGet artifacts contain all advertised TFMs and no private dependencies. ([E04](releases/v0.1.0.md#e04))
- [x] Isolated .NET 10 and .NET 8 consumers restore and execute their compatible NuGet targets; the .NET 8 consumer exercises the portable `netstandard2.0` package. ([E05](releases/v0.1.0.md#e05))
- [x] Unity Mono/IL2CPP claims are withheld until Unity-specific smoke tests pass. ([E03](releases/v0.1.0.md#e03))
- [ ] SDK-level security review includes malformed input, external propagation, and request-scope isolation; consuming hosts enforce token/header coherence. ([E07](releases/v0.1.0.md#e07))

## Delivery and documentation

- [x] `python eng/validate_distribution.py` succeeds. ([E01](releases/v0.1.0.md#e01))
- [x] `python -m unittest discover -s eng -p 'test_*.py'` succeeds. ([E01](releases/v0.1.0.md#e01))
- [x] `python eng/verify_packages.py --version <actual version>` succeeds. ([E04](releases/v0.1.0.md#e04))
- [ ] Release notes, package metadata and `CHANGELOG.md` reflect the exact release. ([E08](releases/v0.1.0.md#e08))
- [x] Portable PDB symbols, license, README, icon, repository metadata and approved dependency inventory are verified in produced packages. ([E04](releases/v0.1.0.md#e04))
- [ ] SourceLink mapping is verified for final release binaries. ([E07](releases/v0.1.0.md#e07))
- [x] A full source SHA and SHA-256 hashes are recorded for six **development CI** package artifacts. ([E06](releases/v0.1.0.md#e06))
- [ ] Revalidate source SHA and all six SHA-256 hashes for the exact approved release-candidate artifacts. ([E08](releases/v0.1.0.md#e08))
- [x] An isolated consumer validates the public NuGet contract independently of Gateway, Identity, or any private host. ([E05](releases/v0.1.0.md#e05))
- [x] Adoption in Gateway and Identity is tracked separately and does not block the public SDK release. ([E03](releases/v0.1.0.md#e03))
- [ ] GitHub Actions workflow for release publication uses OIDC, protected environment and approved tags. ([E08](releases/v0.1.0.md#e08))
- [ ] A release-candidate artifact is reviewed before `v0.1.0` is promoted. ([E08](releases/v0.1.0.md#e08))

## Explicit exclusions

Worker scopes, private tenant registry, tenant authorization policies, persistence isolation, and Unity UPM package publication are **not** v0.1.0 capabilities.
