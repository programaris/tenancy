using System.Net.Http;

using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.Http
{
    /// <summary>Explicitly captures the selected tenant on the outbound request, never inside a pooled handler.</summary>
    public static class TenantHttpRequestExtensions
    {
        public static HttpRequestMessage WithProgramarisTenant(
            this HttpRequestMessage request,
            TenantContext tenant,
            string headerName = "X-Tenant")
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(tenant);

            if (!TenantSlug.TryParse(tenant.Slug, out string slug))
            {
                throw new ArgumentException("Invalid tenant slug.", nameof(tenant));
            }

            if (!IsValidHeaderName(headerName))
            {
                throw new ArgumentException("Invalid tenant header name.", nameof(headerName));
            }

            if (request.Headers.Contains(headerName))
            {
                throw new TenantPropagationException("The tenant header is already set.");
            }

            request.Headers.Add(headerName, slug);
            return request;
        }

        public static HttpRequestMessage WithProgramarisTenant(
            this HttpRequestMessage request,
            ITenantContextAccessor accessor,
            string headerName = "X-Tenant")
        {
            ArgumentNullException.ThrowIfNull(accessor);
            return request.WithProgramarisTenant(accessor.GetRequired(), headerName);
        }

        internal static bool IsValidHeaderName(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            foreach (char c in name)
            {
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') || c == '-'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
