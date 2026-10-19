# Tenant context

`TenantContext` contains a **selected** lowercase tenant slug and an optional stable GUID `TenantId`. It does not prove that the tenant exists or that the caller is authorized.

- `ITenantContextAccessor.Current` returns the selected context or null.
- `ITenantContextAccessor.GetRequired()` throws `TenantContextUnavailableException` when the context is absent.
- The ASP.NET Core implementation has **Scoped** lifetime and its instance is initialized once per HTTP request by the middleware.
- The publicly readable context can be created by consuming code, so only trusted resolution/authorization logic may treat enriched `TenantId` as authoritative.
- Do not inject an HTTP-scoped accessor into singletons or store `Current` across requests.
- Do not assume that ambient HTTP tenant context exists inside background workers or Unity clients.
- The SDK neither propagates an authorization claim nor grants membership or data permissions.

The `TenantContext` positional record is retained in the initial contract. Slug validation is enforced at the HTTP and outbound propagation boundaries by `TenantSlug.TryParse`; constructing a record manually is not authorization and does not bypass the receiving service's checks.
