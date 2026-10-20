namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Outcome of an authenticated principal's access check.</summary>
    public enum TenantAccessStatus
    {
        Granted = 0,
        Denied = 1,
        Unauthenticated = 2,
        Unavailable = 3
    }
}
