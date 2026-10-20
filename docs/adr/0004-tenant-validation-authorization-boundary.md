# ADR 0004 — Selected versus authorized tenant (v0.2.0)

**Status:** Accepted for v0.2.0 development, not yet released.

## Context

`X-Tenant` is user-controlled selection input. `TenantContext` and the v0.1.0 accessor intentionally do not convey membership, tenant existence, or rights. Gateway, Identity and downstream services need provider-neutral checks without importing internal Identity data models.

## Decision

Define separate abstractions `ITenantStore` (existence/status) and `ITenantAccessValidator` (authenticated principal/tenant/operation). Return explicit unavailable outcomes independently of denial. ASP.NET Core registers a separate opt-in middleware and a scoped, write-once `ITenantAuthorizationContextAccessor`; never repurpose `ITenantContextAccessor`. Public endpoints must explicitly opt out. All errors deny execution. Identity is an implementation choice, not a built-in dependency.

## Consequences

- Hosts register trusted providers and arrange authentication, selection and protection in the documented order.
- Gateway may enforce tenant-level access, but downstream APIs independently enforce their business policies and resource isolation.
- Caches are implementation-specific. Never reuse permission results across principals, tenants, operations or security-policy revisions.
- Existing v0.1.0 clients remain source compatible. No default tenant or supertenant logic is introduced.
- PRF changes are postponed until after v0.2.0 is certified.
