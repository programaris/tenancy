# Changelog

All notable changes are recorded here. The project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added for v0.2.0 (development)

- Public `ITenantStore` and `ITenantAccessValidator` contracts with explicit success, denial and outage states.
- Endpoint-scoped, host-defined tenant selection sources for browser requests with duplicate/conflict rejection.
- Separate registry-validated versus membership-authorized contexts.
- Authentication before protected tenant lookup; generic non-enumerating problem responses, host-overridable error policy.
- Optional trusted TenantId in access validation requests for efficient PostgreSQL adapters.
- Explicit Identity consumer integration/contract certification guidance.
- `Programaris.Tenancy.Http` with explicit request capture, approved HTTPS origins, and outbound header validation.
- Authorization, HTTP allowlist and concurrent isolation tests.
- Four-package distribution with synchronized v0.2.0 versioning and SourceLink verification.

**Not yet published.** The v0.1.0 contracts remain available and retain their original selection-only semantics.

## [0.1.0] — Prepared for stable publication

### Added

- Portable `netstandard2.0` contracts and dual-target `netstandard2.0;net10.0` core.
- HTTP `X-Tenant` selection, optional-route metadata and request-scoped tenant accessor.
- Explicit downstream HTTP propagation helper.
- Input and concurrency regression tests.
- .NET 8 isolated consumer certification for the portable NuGet assemblies.
- Strict NuGet package metadata, dependency and portable symbol checks.
- GitHub-hosted cross-platform verification and three-package NuGet artifacts.
- PRF-compatible synchronized versioning and package inventory validation.
- Public getting-started, compatibility, security and release documentation.

### Changed

- Removed the global tenant opt-out; tenant-independent routes must opt out explicitly per endpoint.
- Documented the no-implicit-tenant policy and independent SDK certification.
- Standardized problem response media type as `application/problem+json`.
- Corrected sample path and port in documentation.

Stable package publication requires an approved `v0.1.0` tag, successful tag CI, SourceLink and package integrity certification, and authorized NuGet Trusted Publishing.