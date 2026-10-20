using System.Net.Http;

using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.Http
{
    /// <summary>Copies a tenant selection into an individual outgoing request.</summary>
    public static class TenantHttpRequestExtensions
    {
        internal static readonly HttpRequestOptionsKey<TenantRequestSelection> SelectionKey =
            new HttpRequestOptionsKey<TenantRequestSelection>("Programaris.Tenancy.Http.Selection");

        /// <summary>Captures a tenant for one request without directly writing an untrusted header.</summary>
        public static HttpRequestMessage WithProgramarisTenant(
            this HttpRequestMessage request,
            TenantContext tenant,
            string headerName = "X-Tenant")
        {
            ArgumentNullException.ThrowIfNull(tenant);
            return SetSelection(request, tenant.Slug, headerName, false);
        }

        /// <summary>Captures the supplied request-scoped tenant selection immediately.</summary>
        /// <remarks>Callers must authorize privileged operations separately.</remarks>
        public static HttpRequestMessage WithProgramarisTenant(
            this HttpRequestMessage request,
            ITenantContextAccessor accessor,
            string headerName = "X-Tenant")
        {
            ArgumentNullException.ThrowIfNull(accessor);
            return SetSelection(request, accessor.GetRequired().Slug, headerName, true);
        }

        private static HttpRequestMessage SetSelection(
            HttpRequestMessage request,
            string selectedSlug,
            string headerName,
            bool fromContext)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!TenantSlug.TryParse(selectedSlug, out string slug))
            {
                throw new ArgumentException("Invalid tenant slug.", nameof(selectedSlug));
            }

            if (!IsValidHeaderName(headerName))
            {
                throw new ArgumentException("Invalid tenant header name.", nameof(headerName));
            }

            if (request.Headers.Contains(headerName) ||
                request.Options.TryGetValue(SelectionKey, out _))
            {
                throw new TenantPropagationException("The tenant header or selection is already set.");
            }

            request.Options.Set(SelectionKey, new TenantRequestSelection(slug, headerName, fromContext));
            return request;
        }

        internal static bool IsValidHeaderName(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            foreach (char character in name)
            {
                if (!((character >= 'a' && character <= 'z') ||
                      (character >= 'A' && character <= 'Z') ||
                      (character >= '0' && character <= '9') ||
                      character == '-'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
