# Tenant trust model — v0.2.0 (development)

## Three distinct operations

1. **Selection:** the `X-Tenant` header is parsed into a request-scoped `ITenantContextAccessor`. This is untrusted input.
2. **Validation:** an `ITenantStore` implementation answers whether that tenant exists and is operational.
3. **Authorization:** an `ITenantAccessValidator` implementation evaluates the authenticated principal's permitted tenant access and optionally an operation.

Validation cannot imply authorization. Neither may be inferred from the header, a reverse proxy, or a successful outbound request.

## Adapter boundary

The SDK ships only provider-independent contracts. Identity is the initial authority for tenant membership and access, but consumers provide their own adapters. Do not put Identity endpoints, bearer tokens, database entities, or privileged implementation details in the public package.

`Unavailable` means source failure, not a negative lookup or an authorization grant. A receiving service must fail closed. Cached grants require revocation-aware expiry or invalidation.

The pre-existing `ITenantContextAccessor.Current` keeps its v0.1.0 semantics: **selected**, not authorized.

## Release scope

The HTTP propagation and ASP.NET Core policy integration are separate opt-in capabilities under development. No PRF work is authorized before v0.2.0 is production ready.
