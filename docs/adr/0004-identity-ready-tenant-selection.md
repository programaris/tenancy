# ADR-0004: Endpoint-scoped sources and distinct tenant trust contexts

Status: Accepted for v0.2.0 development — release pending certification.

## Context

OIDC browser redirects and HTML forms do not carry the application's custom
`X-Tenant` header automatically. A header-only selection SDK cannot serve this
scenario, but blindly accepting query/form parameters could allow flow switching.

Anonymous sign-up requires registry validation but never user membership
authorization. A successful registry lookup must not initialize a context
named "authorized". Public tenant error responses can disclose whether an
organization exists, and protected requests must not query the registry
before authentication.

## Decision

- Preserve the v0.1.0 `ITenantContextAccessor`: it remains a request-local,
  untrusted *selection*, never proof of authorization.
- Introduce `ITenantSelectionSource`, enabled via
  `UseProgramarisTenantSelectionSource<TSource>()` metadata on specific
  endpoints only. The host registers the source as its concrete DI type.
  No built-in query/form/cookie selection. Header remains universally accepted.
- SDK rejects invalid, repeated, conflicting selections. The host must verify
  the trustworthiness of each source, including OIDC state/transaction binding,
  anti-forgery protection and redirect validation. Identical selections from
  different approved sources may coexist; duplicate values from one source
  fail closed.
- Introduce `ITenantValidationContextAccessor` for a registry-confirmed
  active tenant. Only initialize `ITenantAuthorizationContextAccessor`
  after `ITenantAccessValidator.Granted`. No public endpoint receives an
  authorization context by bypassing membership checks.
- Protected endpoints require authentication **before registry lookup**.
  Missing/inactive tenant and denied membership have indistinguishable
  default 403 problem bodies on protected endpoints.
- Expose `ITenantFailureResponseWriter` for host-controlled presentation.
  `UseGenericTenantFailures()` maps a missing/inactive public tenant to
  a 403 `tenant_access_unavailable` error. Operational outages remain 503.
  SDK cannot prevent all status/timing side channels in the host application.

## Compatibility

All existing v0.1.0 APIs are preserved. New behavior is opt-in except for
the intentional v0.2 authorization-context and protected-endpoint security
fixes. `TenantAccessRequest.TenantId` is an optional trusted ID supplied by
the registry, never from client headers. Do not infer a tenant default.

## Boundaries

The SDK neither implements PostgreSQL, user/tenant membership, OIDC session
policy, nor public registration allowlists. Those are Identity responsibilities.
Consumers must enforce resource isolation independently of the SDK.
