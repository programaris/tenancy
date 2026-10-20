namespace Programaris.Tenancy.Abstractions
{
    /// <summary>An explicit access decision from a trusted authorization source.</summary>
    public sealed class TenantAccessResult
    {
        private TenantAccessResult(TenantAccessDecision decision)
        {
            Decision = decision;
        }

        /// <summary>Gets the access decision.</summary>
        public TenantAccessDecision Decision { get; }

        /// <summary>Indicates that the trusted validator granted access.</summary>
        public static TenantAccessResult Allowed() => new TenantAccessResult(TenantAccessDecision.Allowed);

        /// <summary>Indicates that the trusted validator rejected access.</summary>
        public static TenantAccessResult Denied() => new TenantAccessResult(TenantAccessDecision.Denied);

        /// <summary>Indicates that a required trusted decision could not be obtained.</summary>
        public static TenantAccessResult Unavailable() => new TenantAccessResult(TenantAccessDecision.Unavailable);
    }
}
