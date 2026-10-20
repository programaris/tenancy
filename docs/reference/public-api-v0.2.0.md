# Public API additions — v0.2.0 development

Stable baseline remains v0.1.0. These types are not yet published as stable.

## Contracts: Programaris.Tenancy.Abstractions

- `ITenantStore.FindAsync(string slug, CancellationToken)` returns
  `TenantValidationResult` (`Found`, `NotFound`, `Inactive`, `Unavailable`).
- `ITenantAccessValidator.ValidateAsync(TenantAccessRequest, CancellationToken)`
  returns `TenantAccessResult` (`Granted`, `Denied`,
  `Unauthenticated`, `Unavailable`).
- `TenantAccessRequest` has `TenantSlug`, `Principal`, `Operation`
  and an optional `TenantId` from the trusted registry.
- `TenantContext(Slug, TenantId?)` already carries optional stable identity.
  `TenantId = Guid.Empty` from a registry is rejected by middleware.
- SDK does not create tenants, memberships or roles.

## ASP.NET Core (net10.0)

- `AddProgramarisTenancy()`, `UseProgramarisTenancy()`,
  `ITenantContextAccessor`: existing v0.1.0 selection API unchanged.
- `ITenantSelectionSource.GetCandidatesAsync(HttpContext, CancellationToken)`:
  host-supplied, request-local candidate extraction. Return zero or one
  canonical slugs. Returning multiple entries, including duplicates, is an
  error. Do not silently collapse repeated query/form values.
- `UseProgramarisTenantSelectionSource<TSource>()`: opt in **per endpoint**
  after DI registration of concrete `TSource`.
- `AddProgramarisTenantAuthorization()` and
  `UseProgramarisTenantAuthorization()`: use after routing, selection and
  authentication, before endpoint execution.
- `ITenantValidationContextAccessor.Current`: trusted active registry tenant,
  not a membership grant.
- `ITenantAuthorizationContextAccessor.Current`: only populated after
  authenticated principal access is granted.
- `AllowUnauthenticatedTenant()`: explicitly public endpoint validates
  existence but **does not** grant membership; use with host-specific public
  operation policies.
- `RequireProgramarisTenantOperation("operation")`: sends operation to the
  host validator.
- `UseGenericTenantFailures()`: opt-in 403 public registry failures.
- `ITenantFailureResponseWriter`/`TenantFailureKind`: replaceable response
  presentation; do not use failure responses to continue endpoint execution.

## Security guarantees and limitations

- Protected anonymous requests are rejected with 401 *before registry queries*.
- Protected unknown/inactive tenants and denied memberships use the same
  403 status and `tenant_access_denied` public response.
- With `UseGenericTenantFailures()`, publicly unknown/inactive tenants
  use generic 403 `tenant_access_unavailable`. Registry/validator outages
  still return 503.
- Even a generic 403 does not eliminate server-time differences or prevent
  discovery by comparing against successful public registration. Rate limiting,
  response equivalence, external sign-up policy and anti-automation are host
  responsibilities.
- Custom selection sources must bind browser parameters to the correct OIDC
  client, redirect URI, state and session; canonical syntax alone is not trust.
- No implicit default tenant and no automatic cross-service header forwarding.

## Slug grammar

`TenantSlug.TryParse` accepts 1–63 lowercase ASCII letters or numbers with
internal hyphens; it rejects leading/trailing hyphens, uppercase letters,
spaces and non-ASCII characters. **Consecutive internal hyphens are valid**
(e.g. `a--b`), preserving published v0.1.0 semantics.
