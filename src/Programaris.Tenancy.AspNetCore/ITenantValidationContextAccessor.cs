using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>A trusted, active tenant from the registry, without any implied user access grant.</summary>
    public interface ITenantValidationContextAccessor
    {
        /// <summary>Gets the registry-validated tenant, or null when unavailable or invalid.</summary>
        TenantContext? Current { get; }
    }
}
