# ADR-0002: `X-Tenant` header convention

- **Status:** Accepted for the source preview; review before first stable release
- **Date:** 2026-10-17

## Decision

Use `X-Tenant` as the default request header. Its value is a lowercase tenant slug such as `programaris` or `acme`. Reject missing required headers, repeated headers, malformed slugs, and silently implied default tenants.

The header name can be configured per host for compatibility with legacy services. A configured alternative is not treated as an additional simultaneously trusted source.

## Alternatives

- `X-Tenant-Slug`: more explicit but diverges from current Gateway/Identity integration conventions.
- GUID-only `X-Tenant`: stable but less convenient for clients; GUID enrichment is an application responsibility.
- Implicit default `programaris`: rejected because failed resolution could operate against the wrong tenant.

## Security implication

This is a **selection** convention only. Authorization belongs to the service receiving the request.