using System.Security.Claims;

namespace Programaris.Tenancy.Abstractions
{
    /// <summary>A request to authorize a principal for a canonical tenant and optional operation.</summary>
    public sealed class TenantAccessRequest
    {
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

        public string TenantSlug { get; }

        public ClaimsPrincipal Principal { get; }

        public string? Operation { get; }
    }
}
