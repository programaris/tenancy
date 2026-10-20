using System;
using System.Security.Claims;

namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Authenticated identity, selected tenant and optional operation to authorize.</summary>
    public sealed class TenantAccessRequest
    {
        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public TenantAccessRequest(string tenantSlug, ClaimsPrincipal principal, string? operation = null)
        {
            TenantSlug = !string.IsNullOrWhiteSpace(tenantSlug)
                ? tenantSlug
                : throw new ArgumentException("Tenant slug is required.", nameof(tenantSlug));
            Principal = principal ?? throw new ArgumentNullException(nameof(principal));
            Operation = operation;
        }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public string TenantSlug { get; }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public ClaimsPrincipal Principal { get; }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public string? Operation { get; }
    }
}
