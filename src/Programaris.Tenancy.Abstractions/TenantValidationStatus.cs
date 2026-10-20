namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Operational state returned by a trusted tenant store.</summary>
    public enum TenantValidationStatus
    {
        Active = 0,
        Inactive = 1,
        NotFound = 2,
        Unavailable = 3
    }
}
