namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Operational state returned by a trusted tenant store.</summary>
    public enum TenantValidationStatus
    {
        /// <summary>Indicates that a tenant exists and is operational.</summary>
        Active = 0,
        /// <summary>Indicates that a tenant is not currently operational.</summary>
        Inactive = 1,
        /// <summary>Indicates that no tenant exists with the requested slug.</summary>
        NotFound = 2,
        /// <summary>Indicates that a required trusted decision could not be obtained.</summary>
        Unavailable = 3
    }
}
