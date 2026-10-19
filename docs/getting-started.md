# Getting started

## Prerequisites

- .NET 10 SDK (builds all included projects and tests).
- An HTTP client such as `curl`, VS Code REST Client, Rider, or Visual Studio.

## Restore, build and test

```bash
dotnet restore Programaris.Tenancy.slnx
dotnet build Programaris.Tenancy.slnx --configuration Release --no-restore -warnaserror
dotnet test Programaris.Tenancy.slnx --configuration Release --no-restore
```

## Run the sample

```bash
dotnet run --project samples/Programaris.Tenancy.Sample.API
curl -i -H 'X-Tenant: programaris' http://localhost:5001/v1/tenant
curl -i -H 'X-Tenant: acme' http://localhost:5001/v1/tenant
curl -i http://localhost:5001/health
curl -i http://localhost:5001/v1/tenant
```

You may alternatively use `samples/Programaris.Tenancy.Sample.API/Programaris.Tenancy.Sample.API.http`.

The `/v1/tenant` response describes the **selected** tenant. It does not authenticate or authorize a caller, nor does it prove the tenant exists in a registry.

## Consume from application code

The host registers services via `AddProgramarisTenancy()` and activates `UseProgramarisTenancy()` after routing. Application classes depend only on `Programaris.Tenancy.Abstractions.ITenantContextAccessor`.

`GetRequired()` throws `TenantContextUnavailableException` when context is unavailable. `Current` returns null. The SDK never selects a tenant from configuration or falls back to a platform tenant. Selectively add `.AllowMissingTenant()` only to tenant-independent endpoints, for example readiness and health checks. This is an endpoint-level exception; a global tenant opt-out is not supported.

Use `AddProgramarisTenant()` only on outbound requests to trusted, approved recipients. **Do not install any automatic global propagation handler.** A receiving service must independently authenticate and authorize the caller and validate the target tenant.

## Build package artifacts without publishing

```bash
dotnet pack Programaris.Tenancy.slnx --configuration Release --output artifacts/nuget
python eng/validate_distribution.py
python eng/verify_packages.py --version 0.1.0-preview.1 --directory artifacts/nuget
```

The package verifier checks metadata, dependency inventory, portable symbols and all advertised targets. The isolated consumer smoke test runs with .NET 10. CI also executes an independent .NET 8 consumer against the portable `netstandard2.0` package. The release checklist includes further validation before publishing.
