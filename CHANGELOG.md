# Changelog

All notable changes are recorded here. The project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added for v0.2.0 (development, not released)

- Provider-neutral tenant store and access validator contracts, including fail-closed unavailable outcomes.
- Per-request authorized tenant context, separate from selection, and opt-in ASP.NET Core protection middleware.
- Operation metadata and integration tests covering inactive/unknown/unauthorized tenants, outages, and concurrency.
- New Programaris.Tenancy.Http package with request-specific tenant capture and exact HTTPS destination allowlists.
- Synchronized four-package NuGet inventory and .NET 8/.NET 10 HTTP consumer support.

### Security

- Never treat tenant selection or forwarding as authentication or access authorization.
- HTTP forwarding rejects conflicting headers and unapproved destinations and disables redirects and cookies in supported primary handlers.
- The SDK provides contracts only; Identity and other trusted authorization adapters remain consumer-owned.


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