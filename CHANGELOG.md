# Changelog

All notable changes are recorded here. The project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Portable `netstandard2.0` contracts and dual-target `netstandard2.0;net10.0` core.
- HTTP `X-Tenant` selection, optional-route metadata and request-scoped tenant accessor.
- Explicit downstream HTTP propagation helper.
- Input and concurrency regression tests.
- GitHub-hosted cross-platform verification and three-package NuGet artifacts.
- PRF-compatible synchronized versioning and package inventory validation.
- Public getting-started, compatibility, security and release documentation.

### Changed

- Removed the global tenant opt-out; tenant-independent routes must opt out explicitly per endpoint.
- Documented the no-implicit-tenant policy and independent SDK certification.
- Standardized problem response media type as `application/problem+json`.
- Corrected sample path and port in documentation.

No production-ready NuGet release is implied by this unreleased change log entry.