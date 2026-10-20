namespace Programaris.Tenancy.Abstractions
{
    /// <summary>An explicit access decision from a trusted authorization source.</summary>
    public sealed class TenantAccessResult
    {
        private TenantAccessResult(TenantAccessDecision decision)
        {
            Decision = decision;
        }

        public TenantAccessDecision Decision { get; }

        public static TenantAccessResult Allowed() => new TenantAccessResult(TenantAccessDecision.Allowed);

        public static TenantAccessResult Denied() => new TenantAccessResult(TenantAccessDecision.Denied);

        public static TenantAccessResult Unavailable() => new TenantAccessResult(TenantAccessDecision.Unavailable);
    }
}
