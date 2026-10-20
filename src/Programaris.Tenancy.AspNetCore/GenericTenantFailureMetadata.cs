namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>
    /// For explicitly anonymous routes, present unavailable tenants as a generic access denial.
    /// It does not authorize public signup or mask source outages.
    /// </summary>
    public sealed class GenericTenantFailureMetadata
    {
    }
}
