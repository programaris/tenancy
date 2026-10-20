using System;
using System.Security.Claims;

namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Authenticated identity, selected tenant and optional operation to authorize.</summary>
    public sealed class TenantAccessRequest
    {
        public TenantAccessRequest(string tenantSlug, ClaimsPrincipal principal, string? operation = null)
        {
            TenantSlug = !string.IsNullOrWhiteSpace(tenantSlug)
                ? tenantSlug
                : throw new ArgumentException("Tenant slug is required.", nameof(tenantSlug));
            Principal = principal ?? throw new ArgumentNullException(nameof(principal));
            Operation = operation;
        }

        public string TenantSlug { get; }

        public ClaimsPrincipal Principal { get; }

        public string? Operation { get; }
    }
}
