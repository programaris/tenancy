namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Outcome of resolving a tenant from a trusted registry.</summary>
    public enum TenantValidationStatus
    {
        /// <summary>Tenant status value.</summary>
        Found = 0,
        /// <summary>Tenant status value.</summary>
        NotFound = 1,
        /// <summary>Tenant status value.</summary>
        Inactive = 2,
        /// <summary>Tenant status value.</summary>
        Unavailable = 3
    }
}
