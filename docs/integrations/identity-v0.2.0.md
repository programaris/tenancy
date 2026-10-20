# Consumer certification: PROGRAMARIS Identity (PR #23)

This describes the contract **Identity must implement**; it does not
include Identity persistence or deployment code.

## Adapter responsibilities

1. Implement `ITenantStore` against the Identity authoritative PostgreSQL
   tenant registry. Return `Found(new TenantContext(canonicalSlug, tenantId))`
   only when active, otherwise `Inactive` or `NotFound`; distinguish outages
   using `Unavailable`. Do not cache unbounded positive grants.
2. Implement `ITenantAccessValidator` using authenticated subject, the
   trusted `TenantId` and optional operation, checking membership, active
   account, roles/permissions and revocation. Deny cross-tenant tokens even
   when a header matches another tenant's slug. On provider failures return
   `Unavailable` or throw; never `Granted`.
3. Register adapters as scoped/transient components and make
   `UseProgramarisTenantAuthorization()` run **after authentication**.
4. For OIDC browser endpoints, implement a registered, concrete
   `ITenantSelectionSource` that validates the protocol-specific source
   and is activated only on explicitly marked endpoints; headers and browser
   selection must agree if both supplied. Validate OIDC state, client,
   redirect URI and session binding; never trust an arbitrary query/form
   `tenant-slug` without those checks.
5. Annotate tenant-independent OIDC discovery, JWKS and health endpoints
   with `AllowMissingTenant()` only where actually appropriate.
6. Public sign-up is explicitly anonymous/registry-validated only.
   Combine `AllowUnauthenticatedTenant()` and
   `UseGenericTenantFailures()` with a **separate Identity allowlist** for
   public registration. Treat closed registration, unknown and inactive
   tenant responses identically. The SDK cannot authorize sign-up.
7. Registration must not create tenants or Owner roles. Invitations and
   existing-user membership are separate verified business flows.

## Recommended middleware order

```csharp
builder.Services.AddProgramarisTenancy();
builder.Services.AddProgramarisTenantAuthorization();
builder.Services.AddScoped<ITenantStore, IdentityTenantRegistryAdapter>();
builder.Services.AddScoped<ITenantAccessValidator, IdentityTenantMembershipAdapter>();
builder.Services.AddScoped<IdentityOidcSelectionSource>();

var app = builder.Build();
app.UseRouting();
app.UseProgramarisTenancy();
app.UseAuthentication();
app.UseProgramarisTenantAuthorization();
app.UseAuthorization();
```

`IdentityOidcSelectionSource` is a host-defined class implementing
`ITenantSelectionSource`; the source must validate browser/OIDC provenance
before returning candidate slugs. Only opt the relevant endpoints in:

```csharp
app.MapMethods("/connect/authorize", new[] { "GET", "POST" }, HandleAuthorization)
   .UseProgramarisTenantSelectionSource<IdentityOidcSelectionSource>();

app.MapPost("/api/v1/authentication/sign-up", HandlePublicRegistration)
   .AllowUnauthenticatedTenant()
   .UseGenericTenantFailures();
```

Examples are schematic: `HandleAuthorization` and `HandlePublicRegistration`
are supplied by the Identity application, not the SDK.

## Certification matrix

| Scenario | Expected outcome |
| --- | --- |
| Header-only API call | Selected from header; never implicit authorization |
| Verified browser OIDC selection | Allowed on explicitly opted-in endpoint |
| Query/form selection on other endpoint | Ignored; still requires header |
| Repeated or conflicting selection | 400; no access granted |
| No selected tenant on required endpoint | 400 |
| Anonymous protected request | 401 before any tenant registry query |
| Public, closed/unknown/inactive tenant | Generic host-defined denial; no writes |
| Trusted tenant active and explicit signup opt-in | Host may create inactive user/membership, no Owner |
| Authenticated subject missing membership | 403 |
| Active tenant with valid membership and required operation | Authorized context populated |
| Cross-tenant token or permission mismatch | Denied by Identity validator |
| PostgreSQL/unavailable Identity provider | 503; no fallback |
| 64 concurrent mixed tenant requests | No cross-request context leakage |
| Disabled tenant or revoked membership after token issue | Deny according to revocation policy |

Release the SDK only after contract tests and an independently certified
Identity integration; this document is not deployment authorization.
