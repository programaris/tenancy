namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Distinguishes an access denial from a failure to obtain a decision.</summary>
    public enum TenantAccessDecision
    {
        Allowed = 0,
        Denied = 1,
        Unavailable = 2
    }
}
