# SDK security review — v0.1.0

**Source review baseline:** `992f4d5d1d2dac6903408860dac34f314afeacf8` (2026-10-19).
**Status:** source-level threat review documented; the release candidate still
requires final regression and provenance certification. This is not an
independent penetration test or a host authorization audit.

## Scope and trust boundary

The public Tenancy SDK transports a **selected** tenant slug. An inbound
`X-Tenant` is untrusted; parsing it never establishes existence, ownership,
membership, access permissions, or identity-provider claims.

| Threat or misuse | SDK boundary | Available evidence / residual responsibility |
| --- | --- | --- |
| Missing tenant silently selects the platform tenant | Default fail-closed HTTP 400; no implicit fallback | `MissingHeaderFailsClosed`, `AllowMissingTenantDoesNotMakeOtherEndpointsOptional`; host must mark tenant-independent endpoints explicitly |
| Malformed, duplicate or combined header bypasses parsing | Strict slug and cardinality checks | `InvalidHeaderIsRejectedEvenOnOptionalRoute`, `DuplicateHeadersAreRejected`, `TenantSlugTests` |
| Tenant data bleeds across concurrent requests | Scoped `TenantContextAccessor` registered in DI | `ConcurrentRequestsPreserveTheirOwnTenant`, sequential isolation tests; out of scope for background-worker scopes |
| Public health endpoint misused to bypass selection | `AllowMissingTenant()` only bypasses presence, not validity | Optional-route invalid-header tests; consumer must independently protect authentication/authorization |
| Outbound propagation overwrites an existing tenant header | Throws on duplicate header, invalid slug or invalid header name | Explicit outbound regression tests; caller remains responsible for downstream destination allowlist |
| Accessor used before resolution | `GetRequired()` throws and outbound helper does not attach a header | Missing-context tests; caller must not infer a default |
| Header conflicts with authenticated tenant claims | SDK deliberately does not authorize | Consuming host must compare claims, membership and selected tenant and reject conflicts |
| Directly created `TenantContext` bypasses syntax validation | Constructor is a data record, not an authorization boundary | Validate application-created values before privileged use; inbound/outbound helpers validate slug |
| Privileged tenant impersonation by selecting `programaris` | No special tenant or supertenant semantics | Host authorization checks remain mandatory |
| Cross-tenant storage read/write | SDK does not implement persistence filters | Host enforces tenant-aware database authorization and isolation |

## Implementation review

- No static ambient tenant context is used; HTTP accessors are scoped per request.
- Tenant selection does not normalize uppercase or trim whitespace to an
  alternate tenant: malformed selection is rejected.
- `TenantResolutionMiddleware` must run after routing so endpoint metadata is
  known and before application handlers that consume tenant context.
- Tenant context propagation is opt-in per outgoing request.
- Configurable header name is validated against ASCII alphanumeric and hyphen syntax.
- No worker tenant scopes, data access isolation, tenant registry, authorization
  or privileged administrative override are implemented in this release.

## Pre-release sign-off gates

1. Pass Windows and Ubuntu CI after the final source-review changes.
2. Validate the actual SourceLink PDB source SHA and file checksums.
3. Re-run trust-boundary tests against the tagged RC.
4. Retain host-specific authentication and authorization checks as separate
   mandatory requirements on each service that adopts this SDK.
5. Record review approval and any remaining exceptions in
   [the v0.1.0 release record](../releases/v0.1.0.md).

**Do not describe this document as certifying Identity, Gateway or any
other consumer.**
