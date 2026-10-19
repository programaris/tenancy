# Release checklist — v0.1.0

> No release is approved by the existence of this file. Every gate must be evidenced by CI or an actual consumer test.

## Public API and behavior

- [ ] Verify stable public contracts for `TenantContext`, `ITenantContextAccessor` and `TenantSlug`.
- [ ] Confirm middleware fail-closed behavior and explicit endpoint exceptions.
- [ ] Confirm no default tenant or global opt-out exists; missing tenant remains absent on optional routes.
- [ ] Confirm malformed, repeated and conflicting tenant headers fail.
- [ ] Confirm authorization remains the consuming application's responsibility.
- [ ] Confirm propagation is opt-in and rejects pre-existing tenant headers.

## Compatibility and quality

- [ ] `dotnet restore Programaris.Tenancy.slnx` succeeds.
- [ ] `dotnet build Programaris.Tenancy.slnx -c Release -warnaserror` succeeds.
- [ ] `dotnet test Programaris.Tenancy.slnx -c Release` succeeds on Windows and Linux.
- [ ] Concurrent request tests complete successfully, with no cross-tenant leakage.
- [ ] NuGet artifacts contain all advertised TFMs and no private dependencies.
- [ ] A clean external consumer installs and runs against each published package's supported target.
- [ ] Unity Mono/IL2CPP claims are withheld until Unity-specific smoke tests pass.
- [ ] SDK-level security review includes malformed input, external propagation, and request-scope isolation; consuming hosts enforce token/header coherence.

## Delivery and documentation

- [ ] `python eng/validate_distribution.py` succeeds.
- [ ] `python -m unittest discover -s eng -p 'test_*.py'` succeeds.
- [ ] `python eng/verify_packages.py --version <actual version>` succeeds.
- [ ] Release notes, package metadata and `CHANGELOG.md` reflect the exact release.
- [ ] SourceLink, symbols, license and README are verified in produced packages.
- [ ] A full source SHA and SHA-256 hashes are recorded for six package artifacts.
- [ ] An isolated consumer validates the public NuGet contract independently of Gateway, Identity, or any private host.
- [ ] Adoption in Gateway and Identity is tracked separately and does not block the public SDK release.
- [ ] GitHub Actions workflow for release publication uses OIDC, protected environment and approved tags.
- [ ] A release-candidate artifact is reviewed before `v0.1.0` is promoted.

## Explicit exclusions

Worker scopes, private tenant registry, tenant authorization policies, persistence isolation, and Unity UPM package publication are **not** v0.1.0 capabilities.
