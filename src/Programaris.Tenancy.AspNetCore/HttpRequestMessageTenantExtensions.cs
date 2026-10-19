using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Opt-in tenant propagation for HTTP requests to approved recipients.</summary>
    public static class HttpRequestMessageTenantExtensions
    {
        /// <summary>Adds the selected tenant to an outbound request without replacing an existing header.</summary>
        public static HttpRequestMessage AddProgramarisTenant(this HttpRequestMessage request, ITenantContextAccessor accessor, string headerName = TenantHeaderOptions.DefaultHeaderName)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(accessor);

            return request.AddProgramarisTenant(accessor.GetRequired(), headerName);
        }

        /// <summary>Adds an explicitly provided tenant slug to an outbound request.</summary>
        /// <remarks>Call only for known, authorized downstream service destinations.</remarks>
        public static HttpRequestMessage AddProgramarisTenant(this HttpRequestMessage request, TenantContext tenant, string headerName = TenantHeaderOptions.DefaultHeaderName)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(tenant);

            if (!TenantHeaderName.IsValid(headerName))
            {
                throw new ArgumentException("The tenant header name is invalid.", nameof(headerName));
            }

            if (!TenantSlug.TryParse(tenant.Slug, out string slug))
            {
                throw new ArgumentException("A canonical tenant slug is required.", nameof(tenant));
            }

            if (request.Headers.Contains(headerName))
            {
                throw new InvalidOperationException("The HTTP request already contains a tenant header.");
            }

            request.Headers.Add(headerName, slug);

            return request;
        }
    }
}