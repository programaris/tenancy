namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Access validation result; transport failure must never mean granted access.</summary>
    public sealed class TenantAccessResult
    {
        private TenantAccessResult(TenantAccessStatus status)
        {
            Status = status;
        }

        public TenantAccessStatus Status { get; }

        public bool IsGranted => Status == TenantAccessStatus.Granted;

        public static TenantAccessResult Granted() => new TenantAccessResult(TenantAccessStatus.Granted);

        public static TenantAccessResult Denied() => new TenantAccessResult(TenantAccessStatus.Denied);

        public static TenantAccessResult Unauthenticated() => new TenantAccessResult(TenantAccessStatus.Unauthenticated);

        public static TenantAccessResult Unavailable() => new TenantAccessResult(TenantAccessStatus.Unavailable);
    }
}
