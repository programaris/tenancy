# PROGRAMARIS Tenancy v0.2.0 — additive public API (development)

This version preserves every v0.1.0 public symbol and the meaning of `ITenantContextAccessor` as **selected**, not authorized.

## Provider-neutral contracts (`Programaris.Tenancy.Abstractions`)

- `ITenantStore.FindAsync(slug, cancellationToken)` returns `TenantValidationResult` with status `Active`, `Inactive`, `NotFound` or `Unavailable`. Only Active includes a trusted `TenantContext` and optional `TenantId`.
- `ITenantAccessValidator.ValidateAsync(TenantAccessRequest, cancellationToken)` returns `TenantAccessResult` with `Allowed`, `Denied` or `Unavailable`.
- `TenantAccessRequest` carries canonical slug, an already-authenticated `ClaimsPrincipal`, and an optional operation. It does not authenticate credentials or imply permission.
- `ITenantAuthorizationContextAccessor.GetRequired()` exposes the active, authorized tenant only after the opt-in ASP.NET Core protection pipeline completes.

The implementations are supplied by consumers. A first-party Identity adapter can call protected Identity APIs; consumers may independently implement databases, services, or cache decorators. No private Identity implementation, internal endpoint, database schema, or implicit system tenant belongs in this public SDK.

## ASP.NET Core integration

```csharp
using Programaris.Tenancy.Abstractions;
using Programaris.Tenancy.AspNetCore;

// Consumer-owned, trusted implementations. Do not use permissive defaults.
builder.Services.AddScoped<ITenantStore, IdentityTenantStore>();
builder.Services.AddScoped<ITenantAccessValidator, IdentityTenantAccessValidator>();

builder.Services.AddProgramarisTenancy();
builder.Services.AddProgramarisTenantProtection();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseProgramarisTenancy();
app.UseProgramarisTenantProtection();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok()).AllowMissingTenant();
app.MapGet("/v1/resources", (ITenantAuthorizationContextAccessor tenants) =>
    Results.Ok(new { tenant = tenants.GetRequired().Tenant.Slug }))
    .RequireProgramarisTenantOperation("resources.read");
```

Order matters. Tenant protection runs **after authentication and selection**, before the handler. Tenant selection still returns HTTP 400 when required but missing or malformed. Protection returns 401 for unauthenticated callers; 404 unknown tenant; 403 inactive/denied; 503 when store/validator is unavailable or inconsistent. All failures use `application/problem+json`. Unexpected provider exceptions fail closed as 503; client cancellation is propagated. A missing provider is a configuration error, never silent authorization.

`AllowMissingTenant()` is an explicit exception for endpoints that do **not** access tenant-specific resources. It bypasses both required selection and protection even when a header is provided. Per-resource authorization remains mandatory in business logic; tenant-level validation does not grant arbitrary operations.

## Outbound HTTP package

```csharp
using Programaris.Tenancy.Http;

builder.Services.AddHttpClient("Identity", client =>
{
    client.BaseAddress = new Uri("https://identity.programaris.com.br/");
}).AddProgramarisTenancyHttp(options =>
{
    options.Mode = TenantPropagationMode.Required;
    options.AllowedOrigins.Add(new Uri("https://identity.programaris.com.br/"));
});

// Construct request in the actual request scope, not inside a pooled DelegatingHandler.
using var request = new HttpRequestMessage(HttpMethod.Get, "v1/users")
    .WithTenantFrom(tenantContextAccessor);
using var response = await httpClient.SendAsync(request, cancellationToken);
```

- `Explicit` (default): only `.WithTenant("slug")`; absent tenant means no header. Context capture is rejected.
- `Required`: `.WithTenant("slug")` or `.WithTenantFrom(scopedAccessor)` is mandatory.
- `Optional`: either selection is forwarded if provided, otherwise no tenant.
- Tenant values belong to one `HttpRequestMessage`; the handler contains no `ITenantContextAccessor`, `HttpContext` or `AsyncLocal`.
- Origins are absolute HTTPS scheme/host/port allowlists, with **no wildcard**, userinfo, query or path. Missing origin means registration fails.
- An existing tenant header, including `DefaultRequestHeaders`, is rejected, never overwritten.
- The builder hook turns off automatic redirects and shared cookies for `HttpClientHandler` and `SocketsHttpHandler`. Unsupported primary-handler types fail registration/creation. A later override of the primary-handler configuration must not re-enable these unsafe behaviors.
- **Forwarding remains selection, not permission.** The receiving service independently authenticates and authorizes the tenant. The transport package never adds credentials.

This package supports .NET 8 and .NET 10. `Programaris.Tenancy.AspNetCore` remains .NET 10-only.
