using System;
using System.Security.Claims;

namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Authenticated identity, selected tenant and optional operation to authorize.</summary>
    public sealed class TenantAccessRequest
    {
        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public TenantAccessRequest(string tenantSlug, ClaimsPrincipal principal, string? operation = null, Guid? tenantId = null)
        {
            TenantSlug = !string.IsNullOrWhiteSpace(tenantSlug)
                ? tenantSlug
                : throw new ArgumentException("Tenant slug is required.", nameof(tenantSlug));
            Principal = principal ?? throw new ArgumentNullException(nameof(principal));
            Operation = operation;
            TenantId = tenantId == Guid.Empty ? throw new ArgumentException("Tenant ID must not be empty.", nameof(tenantId)) : tenantId;
        }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public string TenantSlug { get; }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public ClaimsPrincipal Principal { get; }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public string? Operation { get; }

        /// <summary>Optional stable tenant identity supplied by a trusted registry; never from a header.</summary>
        public Guid? TenantId { get; }
    }
}
