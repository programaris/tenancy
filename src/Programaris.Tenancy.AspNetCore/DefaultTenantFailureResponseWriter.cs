using System.Text.Json;

using Microsoft.AspNetCore.Http;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Default fail-closed HTTP response policy for tenant trust failures.</summary>
    public sealed class DefaultTenantFailureResponseWriter : ITenantFailureResponseWriter
    {
        /// <summary>Writes a generic problem response, preserving HTTP 503 on authority outages.</summary>
        public Task WriteAsync(HttpContext context, TenantFailureKind failure)
        {
            ArgumentNullException.ThrowIfNull(context);

            bool publicTenant = context.GetEndpoint()?.Metadata.GetMetadata<AllowUnauthenticatedTenantMetadata>() is not null;
            bool generic = context.GetEndpoint()?.Metadata.GetMetadata<GenericTenantFailureMetadata>() is not null;

            (int status, string code) = failure switch
            {
                TenantFailureKind.MissingTenant =>
                    (StatusCodes.Status400BadRequest, "tenant_header_missing"),

                TenantFailureKind.AuthenticationRequired =>
                    (StatusCodes.Status401Unauthorized, "tenant_authentication_required"),

                TenantFailureKind.NotFoundOrInactive when publicTenant && !generic =>
                    (StatusCodes.Status404NotFound, "tenant_not_found"),

                TenantFailureKind.NotFoundOrInactive =>
                    (StatusCodes.Status403Forbidden, generic ? "tenant_access_unavailable" : "tenant_access_denied"),

                TenantFailureKind.AccessDenied =>
                    (StatusCodes.Status403Forbidden, generic ? "tenant_access_unavailable" : "tenant_access_denied"),

                TenantFailureKind.RegistryUnavailable =>
                    (StatusCodes.Status503ServiceUnavailable, "tenant_registry_unavailable"),

                TenantFailureKind.AuthorizationUnavailable =>
                    (StatusCodes.Status503ServiceUnavailable, "tenant_authorization_unavailable"),

                _ => (StatusCodes.Status503ServiceUnavailable, "tenant_authorization_unavailable")
            };

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(new { type = "about:blank", title = code, status, code }),
                context.RequestAborted);
        }
    }
}
