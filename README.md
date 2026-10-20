# PROGRAMARIS Tenancy

**Public .NET contracts and integration helpers for consuming multi-tenant PROGRAMARIS services.**

[![CI](https://github.com/programaris/tenancy/actions/workflows/ci.yml/badge.svg)](https://github.com/programaris/tenancy/actions/workflows/ci.yml)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache--2.0-blue.svg)](LICENSE)

> [!IMPORTANT]
> **Release policy:** v0.1.0 is the published stable baseline. The development branch targets v0.2.0; CI packages are non-stable `0.2.0-dev.N` artifacts. A successful development build is not permission to publish a release.

PROGRAMARIS uses its own services as a tenant. External consumers use the same public tenant-selection convention. This repository contains client-facing contracts, integration helpers, samples, and documentation, **not** tenant administration, identity-provider implementation, or authorization rules.

## SDK capabilities

- Parse a selected tenant from the HTTP `X-Tenant` header (lowercase slug).
- Read the selected tenant through a request-scoped `ITenantContextAccessor`.
- Explicitly allow tenant-less endpoints such as health probes.
- Explicitly propagate the selected tenant to known downstream HTTP services.
- Reject malformed, missing mandatory, and repeated tenant headers.
- Never infer a default tenant or disable tenant requirements globally; only explicit `AllowMissingTenant()` endpoints can omit a tenant.
- Keep tenant **selection** separate from **authentication and authorization**.
- v0.2.0: bring your own `ITenantStore` and `ITenantAccessValidator` provider implementations.
- v0.2.0: opt-in ASP.NET Core middleware enforcing registry checks and authenticated access, failing closed when a source is unavailable.
- v0.2.0: opt-in named HttpClient package with exact-origin HTTPS restrictions, disabled redirects, and explicit per-request context capture.

### Not provided

Tenant administration, built-in Identity membership implementations, background-worker scopes, automatic ambient HTTP propagation, persistence isolation, and Unity UPM packaging. Authorization **enforcement** is provided only when explicitly enabled with trusted host adapters.

## Packages and compatibility

| NuGet package | Target frameworks | Purpose |
| --- | --- | --- |
| `Programaris.Tenancy.Abstractions` | `netstandard2.0` | Tenant selection plus provider-independent validation and access contracts |
| `Programaris.Tenancy` | `netstandard2.0`, `net10.0` | Slug validation, core exceptions |
| `Programaris.Tenancy.AspNetCore` | `net10.0` | Tenant selection and opt-in authorized request context |
| `Programaris.Tenancy.Http` | `net8.0`, `net10.0` | Restricted outbound HttpClient requests |

Building the portable targets does **not** certify runtime compatibility with Unity Mono/IL2CPP. See [Compatibility](docs/compatibility.md).

## Build and test from source

Requires the .NET 10 SDK.

```bash
dotnet restore Programaris.Tenancy.slnx
dotnet build Programaris.Tenancy.slnx --configuration Release --no-restore -warnaserror
dotnet test Programaris.Tenancy.slnx --configuration Release --no-restore
```

Run the API sample:

```bash
dotnet run --project samples/Programaris.Tenancy.Sample.API
```

```bash
curl -i -H 'X-Tenant: programaris' http://localhost:5001/v1/tenant
curl -i -H 'X-Tenant: acme' http://localhost:5001/v1/tenant
curl -i http://localhost:5001/health
curl -i http://localhost:5001/v1/tenant
```

The final request should return HTTP 400 with `tenant_header_missing`. A successful tenant echo is **not** evidence of authorization.

## ASP.NET Core inbound integration

```csharp
using Programaris.Tenancy.Abstractions;
using Programaris.Tenancy.AspNetCore;

builder.Services.AddProgramarisTenancy();

var app = builder.Build();

app.UseRouting();
app.UseProgramarisTenancy();

app.MapGet("/health", () => Results.Ok()).AllowMissingTenant();
app.MapGet("/v1/tenant", (ITenantContextAccessor tenants) =>
    Results.Ok(new { slug = tenants.GetRequired().Slug }));
```

This preserves the v0.1.0 selection-only flow. To enforce tenant access, register your own `ITenantStore` and `ITenantAccessValidator` (for example, Identity-backed adapters), call `AddProgramarisTenantAuthorization()`, and place `UseProgramarisTenantAuthorization()` **after** routing, tenant resolution, and authentication but **before** endpoint execution. Consumers may inspect `ITenantAuthorizationContextAccessor.Current` only after a grant. `AllowUnauthenticatedTenant()` explicitly skips membership authorization for an endpoint but still validates tenant existence. `AllowMissingTenant()` permits tenant-less endpoints without authorizing any tenant.

## Outbound propagation (explicit)

```csharp
using var request = new HttpRequestMessage(HttpMethod.Get, "v1/profile");
request.AddProgramarisTenant(tenantContextAccessor.GetRequired());
using var response = await httpClient.SendAsync(request, cancellationToken);
```

Only propagate to approved internal services. Do not forward an untrusted inbound `X-Tenant` to external destinations or treat it as an authorization assertion.

## Outbound propagation (restricted v0.2.0)

```csharp
services.AddHttpClient("Identity")
    .AddProgramarisTenancyHttp(options =>
    {
        options.Mode = TenantPropagationMode.Required;
        options.AllowedOrigins.Add(new Uri("https://identity.programaris.com.br"));
    });
```

For a request, call `request.WithProgramarisTenant(tenantContext)` explicitly. Prefer a **validated and authorized** tenant context when invoking privileged downstream operations. HttpClientFactory handler scopes are not request scopes; the SDK never reads `ITenantContextAccessor` from pooled handlers. See the [v0.2 HTTP guide](docs/getting-started-http-v0.2.0.md).

## Documentation

- [Documentation index](docs/README.md)
- [Getting started](docs/getting-started.md)
- [HTTP contract](docs/reference/http-contract.md)
- [Explicit tenant context ADR](docs/adr/0003-explicit-tenant-context.md)
- [Trust boundaries](docs/security/trust-boundaries.md)
- [Release checklist](docs/release-checklist.md)
- [PRF conventions](docs/prf-integration.md)

## Contribution and support

Public PRs run on GitHub-hosted runners. Please see [CONTRIBUTING.md](CONTRIBUTING.md), [SECURITY.md](SECURITY.md), [SUPPORT.md](SUPPORT.md), and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## License

[Apache License, Version 2.0](LICENSE). See [NOTICE](NOTICE).
