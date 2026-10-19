# ADR-0001: Public SDK boundary

- **Status:** Accepted for the repository bootstrap
- **Date:** 2026-10-17

## Context

PROGRAMARIS is a tenant of its own services. External and internal consumers need a consistent protocol for selecting tenant context without requiring access to private services or implementation details.

## Decision

The `programaris/tenancy` repository publishes only client-facing contracts, integration helpers, samples and documentation. It remains identity-provider-agnostic. Authorization, tenant registry administration and confidential infrastructure implementations remain in private service repositories.

## Consequences

- The codebase is suitable for public collaboration from its first commit.
- The SDK cannot attest to tenant authorization; consumers must enforce it.
- Private applications may use public SDKs, but public SDKs must not depend on private application packages.
