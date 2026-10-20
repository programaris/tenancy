namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Outcome of resolving a tenant from a trusted registry.</summary>
    public enum TenantValidationStatus
    {
        Found = 0,
        NotFound = 1,
        Inactive = 2,
        Unavailable = 3
    }
}
