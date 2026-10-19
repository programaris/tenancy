# HTTP contract — v0.1.0 candidate

| Item | Contract |
| --- | --- |
| Header | `X-Tenant` by default; configurable by host |
| Value | Lowercase ASCII slug, 1–63 characters |
| Syntax | `a-z`, `0-9`, optional internal `-`; starts/ends alphanumeric |
| Repeated or comma-combined values | HTTP 400 `tenant_header_invalid` |
| Missing mandatory header | HTTP 400 `tenant_header_missing` |
| Present but malformed/empty header | HTTP 400 `tenant_header_invalid` |
| Endpoint with `AllowMissingTenant()` | Missing header permitted, invalid present header still rejected |
| `RequireTenant = false` | Missing header permitted globally, present header still validated |
| Body media type on parsing error | `application/problem+json` |

```http
GET /v1/example HTTP/1.1
X-Tenant: programaris
```

An invalid header produces a standardized problem-compatible body:

```json
{
  "type": "about:blank",
  "title": "Invalid tenant header",
  "status": 400,
  "code": "tenant_header_invalid"
}
```

A GUID-shaped string may also satisfy the slug character rules; the SDK does not infer identifier type or tenant existence from the syntax. Existing GUID-based `X-Tenant` contracts require an explicit compatibility decision.

This is only syntax validation. Applications handle authentication, token/header coherence, tenant existence, tenant operational status, membership and per-resource authorization. A syntactically valid tenant slug must never independently authorize access.

## Outgoing requests

`HttpRequestMessage.AddProgramarisTenant(tenantContext)` explicitly adds the selected `X-Tenant` header. Passing an invalid slug or header name throws `ArgumentException`; passing an existing header throws `InvalidOperationException` instead of silently replacing it. The extension does not automatically attach headers, validate downstream recipient identity, or sign tenant assertions.

## Middleware placement

Call `UseRouting()` before `UseProgramarisTenancy()` so endpoint metadata is available. Authenticate and authorize before invoking protected endpoint handlers according to the host's trust model. The SDK intentionally has no implicit cross-tenant elevated access.
