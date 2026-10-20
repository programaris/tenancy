namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Outcome of an authenticated principal's access check.</summary>
    public enum TenantAccessStatus
    {
        /// <summary>Tenant status value.</summary>
        Granted = 0,
        /// <summary>Tenant status value.</summary>
        Denied = 1,
        /// <summary>Tenant status value.</summary>
        Unauthenticated = 2,
        /// <summary>Tenant status value.</summary>
        Unavailable = 3
    }
}
