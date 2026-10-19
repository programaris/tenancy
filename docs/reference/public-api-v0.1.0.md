# Public API review — PROGRAMARIS Tenancy v0.1.0

**Review baseline:** `992f4d5d1d2dac6903408860dac34f314afeacf8` (`dev`, 2026-10-19).
**Status:** documented pre-release contract; revalidate the tagged release candidate.
This page describes the public SDK, not tenant membership or authorization.

## Contract surface

| Package | Public type or member | Semantics |
| --- | --- | --- |
| `Programaris.Tenancy.Abstractions` | `sealed record TenantContext(string Slug, Guid? TenantId = null)` | Selection value only, not proof of authorization |
| Abstractions | `ITenantContextAccessor.Current : TenantContext?` | Nullable selected context |
| Abstractions | `ITenantContextAccessor.GetRequired() : TenantContext` | Throws when context absent |
| `Programaris.Tenancy` | `TenantSlug.TryParse(string? value, out string slug) : bool` | Strict lowercase ASCII, 1–63 chars |
| Core | `TenantContextUnavailableException()` | Signals missing required context |
| `Programaris.Tenancy.AspNetCore` | `TenantHeaderOptions.DefaultHeaderName = "X-Tenant"` | Canonical HTTP selector |
| AspNetCore | `TenantHeaderOptions.HeaderName : string` | Optional host-specific header name |
| AspNetCore | `TenantContextAccessor.Current`, `GetRequired()` | Request-scoped, read-only for consumers |
| AspNetCore | `TenantResolutionMiddleware(RequestDelegate, IOptions<TenantHeaderOptions>)` | Internal tenant selection stage exposed for ASP.NET Core middleware |
| AspNetCore | `TenantResolutionMiddleware.InvokeAsync(HttpContext, TenantContextAccessor)` | Parses header and returns HTTP 400 on invalid/missing selection |
| AspNetCore | `AllowMissingTenantMetadata` | Marks a tenant-independent endpoint |
| AspNetCore | `AddProgramarisTenancy(IServiceCollection, Action<TenantHeaderOptions>? = null)` | Registers scoped tenant selector |
| AspNetCore | `UseProgramarisTenancy(IApplicationBuilder)` | Adds middleware after routing |
| AspNetCore | `AllowMissingTenant<TBuilder>(TBuilder) where TBuilder : IEndpointConventionBuilder` | Per-endpoint opt-out of header presence |
| AspNetCore | `AddProgramarisTenant(HttpRequestMessage, TenantContext, string headerName = "X-Tenant")` | Explicit outbound propagation |
| AspNetCore | `AddProgramarisTenant(HttpRequestMessage, ITenantContextAccessor, string headerName = "X-Tenant")` | Explicit outbound propagation from required context |

Extension methods above are defined in `ProgramarisTenancyExtensions` and
`HttpRequestMessageTenantExtensions`, respectively. The `TenantContextAccessor.Initialize`
method is **internal**, not a caller-facing initialization contract.

## Compatibility boundaries

- `Programaris.Tenancy.Abstractions`: `netstandard2.0`.
- `Programaris.Tenancy`: `netstandard2.0` and `net10.0`.
- `Programaris.Tenancy.AspNetCore`: `net10.0`.
- `TenantContext` constructor accepts a string but does not validate its syntax;
  validation occurs at the HTTP inbound/outbound boundaries. Application-created
  tenant values must be independently validated when used outside HTTP.
- The SDK has **no default tenant**, **no global `RequireTenant` toggle**, and
  **no automatic outbound propagation**.
- `AllowMissingTenant()` applies to endpoint **presence** checks only.
  Invalid supplied headers remain invalid, and this metadata does not bypass authorization.
- Tenant existence, operating state, membership, claims coherence, persistence
  isolation and privileged access remain the consuming application's responsibility.
- Unity Mono and IL2CPP are **not certified** by the `netstandard2.0` target alone.

## Freeze criteria

This v0.1.0 API is intended to remain stable across `0.1.x` releases.
Removal, signature changes or new implicit behavior require an explicit compatibility
review. Release approval requires rechecking this inventory against actual RC binaries,
not merely reviewing source text.
