using System.Security.Claims;

namespace Programaris.Tenancy.Abstractions
{
    /// <summary>A request to authorize a principal for a canonical tenant and optional operation.</summary>
    public sealed class TenantAccessRequest
    {
        /// <summary>Creates an authorization request using an authenticated principal.</summary>
        public TenantAccessRequest(string tenantSlug, ClaimsPrincipal principal, string? operation = null)
        {
            if (string.IsNullOrWhiteSpace(tenantSlug))
            {
                throw new ArgumentException("A tenant slug is required.", nameof(tenantSlug));
            }

            TenantSlug = tenantSlug;
            Principal = principal ?? throw new ArgumentNullException(nameof(principal));
            Operation = operation;
        }

        /// <summary>Gets the canonical slug of the requested tenant.</summary>
        public string TenantSlug { get; }

        /// <summary>Gets the authenticated principal being evaluated.</summary>
        public ClaimsPrincipal Principal { get; }

        /// <summary>Gets the operation against which tenant access was validated.</summary>
        public string? Operation { get; }
    }
}
