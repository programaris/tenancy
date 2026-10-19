# Documentation

- [Getting started](getting-started.md) — source checkout, sample API and packing.
- [Compatibility](compatibility.md) — .NET and Unity support boundaries.
- [Architecture](architecture/overview.md) — modular design and lifecycle.
- [Tenant context](concepts/tenant-context.md) — accessor contract and scope.
- [HTTP contract](reference/http-contract.md) — header rules and problem responses.
- [Public API v0.1.0](reference/public-api-v0.1.0.md) — signatures and compatibility guarantees.
- [Trust boundaries](security/trust-boundaries.md) — selection versus authorization.
- [SDK security review](security/sdk-security-review-v0.1.0.md) — assessed threats and residual host responsibilities.
- [ADR-0001](adr/0001-public-sdk-boundary.md) — public SDK boundary.
- [ADR-0002](adr/0002-header-convention.md) — `X-Tenant` convention.
- [ADR-0003](adr/0003-explicit-tenant-context.md) — no implicit default tenant or global opt-out.
- [PRF integration](prf-integration.md) — build-version and package manifest alignment.
- [Release policy](release-policy.md) — versioning and promotion.
- [Release checklist](release-checklist.md) — requirements before `v0.1.0`.
- [v0.1.0 certification record](releases/v0.1.0.md) — evidence register and remaining release gates.

This documentation describes the source preview and its intended v0.1.0 feature scope. A stable release must pass the checklist and publish matching packages.
