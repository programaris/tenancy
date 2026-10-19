# ADR-0003: Explicit tenant context

Status: Accepted

## Context

A multi-tenant request without an explicit tenant selection must not silently
inherit one from configuration, platform defaults, or previous requests.

## Decision

- The SDK never selects a default tenant.
- Tenant-aware HTTP endpoints require an explicit canonical tenant slug.
- A missing selection fails closed with HTTP 400 `tenant_header_missing`.
- Only tenant-independent endpoints annotated with `AllowMissingTenant()` may
  omit the header; there is no global opt-out.
- If a tenant header is supplied, its syntax is validated even on optional endpoints.
- The request-scoped accessor remains unset when a tenant is absent.
- The selected tenant is untrusted input, not authentication or authorization.
- Consumers must authorize caller-to-tenant access and enforce data isolation.
- The public SDK remains independent of Gateway, Identity and other consumers.

## Consequences

Consumers explicitly select a tenant or deliberately declare an endpoint
tenant-independent. Existing hosts migrate separately without changing this
public contract. SDK release certification does not require adoption by
specific host services.

This decision does not add tenant registration, authorization rules, persistence
filters, or background worker scopes to the 0.1.0 SDK.
