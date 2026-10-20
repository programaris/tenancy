using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Provides only the successfully authorized tenant for this HTTP request.</summary>
    public interface ITenantAuthorizationContextAccessor
    {
        TenantContext? Current { get; }
    }
}
