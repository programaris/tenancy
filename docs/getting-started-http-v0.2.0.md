# Outbound tenant HTTP SDK — v0.2.0

This opt-in package targets .NET 10 initially. Install `Programaris.Tenancy.Http` alongside the existing Tenancy packages.

```csharp
services.AddHttpClient("Identity")
    .AddProgramarisTenancyHttp(options =>
    {
        options.Mode = TenantPropagationMode.Required;
        options.AllowedOrigins.Add(new Uri("https://identity.programaris.com.br"));
    });
```

Within the consumer's request scope, explicitly capture the selected tenant:

```csharp
using var request = new HttpRequestMessage(HttpMethod.Get, "https://identity.programaris.com.br/v1/users");
request.WithProgramarisTenant(tenantAccessor);
using var response = await client.SendAsync(request, cancellationToken);
```

The handler **does not resolve scoped tenant accessors**: `IHttpClientFactory` pools handlers across application scopes. A request must capture the tenant explicitly; `Required` refuses missing selections, `Explicit` accepts only a directly provided `TenantContext`, and `Optional` permits no selection. No mode authorizes access to a tenant.

Only exact HTTPS origins (host and port) are accepted. Configuration is checked during registration. Tenant selection is stored in `HttpRequestMessage.Options`, and the handler adds the header only after checking the destination. Pre-existing tenant headers are rejected. Automatic redirects and pooled cookies are disabled by the primary handler. Do not override the primary handler after `AddProgramarisTenancyHttp`; doing so can re-enable redirects and bypass origin checks. Do not use a client containing prepopulated `DefaultRequestHeaders` tenant values. The receiving service must authenticate and authorize independently.
