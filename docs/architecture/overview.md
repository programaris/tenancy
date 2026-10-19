# Architecture

## Purpose

Public integration support for clients of PROGRAMARIS multi-tenant APIs. Public packages do not depend on proprietary identity providers, tenant registries, or private persistence models.

```text
HTTP request: X-Tenant: tenant-slug (untrusted input)
      |
      v
ASP.NET Core middleware (syntax validation, no authorization)
      |
      v
Request-scoped ITenantContextAccessor
      |                       |
      v                       v
Application code        Explicit HTTP propagation
(authorizes separately)  (known recipients only)
```

## Packages

- **Abstractions (`netstandard2.0`)**: portable `TenantContext` and `ITenantContextAccessor`.
- **Core (`netstandard2.0;net10.0`)**: canonical slug validation and missing-context exception.
- **AspNetCore (`net10.0`)**: options, header middleware, request scope and outbound HTTP request helpers.

## Context lifecycle

1. Routing resolves endpoint metadata.
2. Middleware looks for the configured header.
3. Invalid headers fail with HTTP 400; absence fails by default unless explicitly allowed.
4. A DI-scoped accessor is initialized once and made available to downstream code.
5. The host authenticates and authorizes the caller and enforces tenant-aware resource isolation.
6. The request DI scope is disposed at completion, including exceptional paths.

## Deliberate limits

- A selected tenant is not a trusted authorization assertion.
- `TenantId` is optional; the SDK does not perform tenant-registry lookups.
- Endpoint metadata only controls whether a header is required, not user authentication.
- Outbound propagation is opt-in; the consumer must choose the recipient.
- Workers, EF Core filters, Redis, messaging and administrative cross-tenant operations remain out of scope for v0.1.0.

## Release discipline

The public `distribution.json` declares exactly three NuGet packages with synchronized versioning. CI builds, tests and packs them on GitHub-hosted runners without publishing. See [PRF integration](../prf-integration.md).
