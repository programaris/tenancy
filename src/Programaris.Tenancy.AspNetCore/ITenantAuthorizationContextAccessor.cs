using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Provides only the successfully authorized tenant for this HTTP request.</summary>
    public interface ITenantAuthorizationContextAccessor
    {
        /// <summary>Gets a previously authorized tenant, or null when access has not been granted.</summary>
        TenantContext? Current { get; }
    }
}
