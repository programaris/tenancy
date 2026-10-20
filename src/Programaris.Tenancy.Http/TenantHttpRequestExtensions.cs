using Programaris.Tenancy.Abstractions;
using Programaris.Tenancy.Http.Internal;

namespace Programaris.Tenancy.Http
{
    /// <summary>Attaches a tenant to a single outbound request; never mutates ambient state.</summary>
    public static class TenantHttpRequestExtensions
    {
        /// <summary>Attaches an explicit canonical slug; rejects a second selection.</summary>
        public static HttpRequestMessage WithTenant(this HttpRequestMessage request, string tenantSlug)
        {
            return SetSelection(request, tenantSlug, false);
        }

        /// <summary>Copies the current scoped selection now, outside the pooled HTTP handler.</summary>
        /// <remarks>This captures selection only, not tenant authorization. Validate access separately.</remarks>
        public static HttpRequestMessage WithTenantFrom(this HttpRequestMessage request, ITenantContextAccessor accessor)
        {
            ArgumentNullException.ThrowIfNull(accessor);

            return SetSelection(request, accessor.GetRequired().Slug, true);
        }

        private static HttpRequestMessage SetSelection(HttpRequestMessage request, string tenantSlug, bool fromContext)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!TenantSlug.TryParse(tenantSlug, out string canonical))
            {
                throw new ArgumentException("A canonical tenant slug is required.", nameof(tenantSlug));
            }

            if (request.Options.TryGetValue(TenantRequestOptionsKeys.Selection, out _))
            {
                throw new TenantPropagationException("The request already has a tenant selection.");
            }

            request.Options.Set(TenantRequestOptionsKeys.Selection, new TenantRequestSelection(canonical, fromContext));

            return request;
        }
    }
}
