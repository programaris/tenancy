namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Access validation result; transport failure must never mean granted access.</summary>
    public sealed class TenantAccessResult
    {
        private TenantAccessResult(TenantAccessStatus status)
        {
            Status = status;
        }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public TenantAccessStatus Status { get; }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public bool IsGranted => Status == TenantAccessStatus.Granted;

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public static TenantAccessResult Granted() => new TenantAccessResult(TenantAccessStatus.Granted);

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public static TenantAccessResult Denied() => new TenantAccessResult(TenantAccessStatus.Denied);

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public static TenantAccessResult Unauthenticated() => new TenantAccessResult(TenantAccessStatus.Unauthenticated);

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public static TenantAccessResult Unavailable() => new TenantAccessResult(TenantAccessStatus.Unavailable);
    }
}
