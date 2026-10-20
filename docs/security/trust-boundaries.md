# Trust boundaries

## Non-negotiable rule

An inbound `X-Tenant` value is **untrusted input**. The SDK parses and carries it; it does not authenticate the caller, authorize the tenant, or grant data access.

## Responsibilities of the receiving service

1. Authenticate the caller when the endpoint requires it.
2. Resolve/validate tenant existence and operational status against trusted sources.
3. Check that the caller is permitted to act for the requested tenant.
4. Apply tenant-aware data authorization and isolation in every access path.
5. Reject conflicting or untrusted claims/headers rather than silently picking one.
6. Remove externally supplied privileged/internal context headers at trust boundaries.
7. Never treat a platform-owned tenant (e.g. `programaris`) as a supertenant.

## v0.2 trusted validation and access

The opt-in `ITenantStore` and `ITenantAccessValidator` are implemented by the host against trusted sources. The middleware rejects unavailable stores/validators without granting access, and exposes a separate `ITenantAuthorizationContextAccessor` only after successful checks. `ITenantContextAccessor.Current` remains **untrusted selection**; it must not be used as proof of membership.

An explicit `AllowUnauthenticatedTenant()` endpoint still requires a real, active tenant but skips membership checks. It sets **only** `ITenantValidationContextAccessor.Current`, never `ITenantAuthorizationContextAccessor.Current`. Use it only for intentionally public resources. Real authorization rules and resource-level isolation stay in the host.

## Browser selection and enumeration

Non-header selection is permitted only through explicit per-endpoint host sources.
The SDK checks duplicates and conflicts but cannot establish OIDC state, session
binding, client authorization or safe redirects. The host must authenticate its
sources. Protected requests authenticate before a registry query; protected
missing/inactive and denied access share a default 403. Public endpoints may
opt into generic 403 failures, or replace the response writer; neither mitigates
timing attacks, user enumeration or signup abuse without host protection.

## HTTP propagation

The helper does **not** automatically copy headers between requests. Call `AddProgramarisTenant` only when sending to a known, approved downstream endpoint. In v0.2.0, the new HttpClient helper accepts only explicitly allowed HTTPS origins, disables automatic redirects, and never reads an ambient tenant from pooled handlers. Do not override the configured primary transport handler to re-enable redirects. A receiving service still has to perform the checks above; network location and `X-Tenant` alone are never proof of authorization.

## Endpoint opt-out

Only endpoints that explicitly do not require tenant selection should use `AllowMissingTenant()`. It bypasses a header **presence** requirement, not authentication or authorization.

## Public / private boundary

This repository may contain transport and client contracts. Do not add internal connection strings, privileged tokens, membership bypass logic, private tenant registries, or internal authorization implementation.

## Reporting vulnerabilities

See the root [SECURITY.md](../../SECURITY.md). Never disclose exploit details in a public issue.
