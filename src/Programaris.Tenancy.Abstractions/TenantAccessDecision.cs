namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Distinguishes an access denial from a failure to obtain a decision.</summary>
    public enum TenantAccessDecision
    {
        /// <summary>Indicates that the trusted validator granted access.</summary>
        Allowed = 0,
        /// <summary>Indicates that the trusted validator rejected access.</summary>
        Denied = 1,
        /// <summary>Indicates that a required trusted decision could not be obtained.</summary>
        Unavailable = 2
    }
}
