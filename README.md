# PROGRAMARIS Tenancy

**Public .NET contracts and integration helpers for consuming multi-tenant PROGRAMARIS services.**

[![CI](https://github.com/programaris/tenancy/actions/workflows/ci.yml/badge.svg)](https://github.com/programaris/tenancy/actions/workflows/ci.yml)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache--2.0-blue.svg)](LICENSE)

> [!IMPORTANT]
> **Pre-release source: not yet certified or published as a production-ready package.**
> Package version `0.1.0-preview.1` is the local build default; CI creates `0.1.0-dev.N` artifacts. The `0.1.0` release requires passing the [release checklist](docs/release-checklist.md).

PROGRAMARIS uses its own services as a tenant. External consumers use the same public tenant-selection convention. This repository contains client-facing contracts, integration helpers, samples, and documentation, **not** tenant administration, identity-provider implementation, or authorization rules.

## Supported scope in 0.1.0

- Parse a selected tenant from the HTTP `X-Tenant` header (lowercase slug).
- Read the selected tenant through a request-scoped `ITenantContextAccessor`.
- Explicitly allow tenant-less endpoints such as health probes.
- Explicitly propagate the selected tenant to known downstream HTTP services.
- Reject malformed, missing mandatory, and repeated tenant headers.
- Never infer a default tenant or disable tenant requirements globally; only explicit `AllowMissingTenant()` endpoints can omit a tenant.
- Keep tenant **selection** separate from **authentication and authorization**.

### Not provided

Tenant registration, user membership checks, administrative cross-tenant access, background-worker scopes, automatic HTTP propagation, persistence isolation, Unity UPM packaging, or an authorization middleware.

## Packages and compatibility

| NuGet package | Target frameworks | Purpose |
| --- | --- | --- |
| `Programaris.Tenancy.Abstractions` | `netstandard2.0` | `TenantContext`, `ITenantContextAccessor` |
| `Programaris.Tenancy` | `netstandard2.0`, `net10.0` | Slug validation, core exceptions |
| `Programaris.Tenancy.AspNetCore` | `net10.0` | Inbound HTTP middleware and opt-in outbound helpers |

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

Place authentication/authorization middleware according to the host's security architecture. Tenant middleware only selects a tenant; it never verifies whether the caller may access it.

## Outbound propagation (explicit)

```csharp
using var request = new HttpRequestMessage(HttpMethod.Get, "v1/profile");
request.AddProgramarisTenant(tenantContextAccessor.GetRequired());
using var response = await httpClient.SendAsync(request, cancellationToken);
```

Only propagate to approved internal services. Do not forward an untrusted inbound `X-Tenant` to external destinations or treat it as an authorization assertion.

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
