namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Trusted tenant lookup result; never represents caller authorization.</summary>
    public sealed class TenantValidationResult
    {
        private TenantValidationResult(TenantValidationStatus status, TenantContext? tenant)
        {
            Status = status;
            Tenant = tenant;
        }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public TenantValidationStatus Status { get; }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public TenantContext? Tenant { get; }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public bool IsValid => Status == TenantValidationStatus.Found;

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public static TenantValidationResult Found(TenantContext tenant)
        {
            if (tenant is null)
            {
                throw new ArgumentNullException(nameof(tenant));
            }

            return new TenantValidationResult(TenantValidationStatus.Found, tenant);
        }

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public static TenantValidationResult NotFound() => new TenantValidationResult(TenantValidationStatus.NotFound, null);

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public static TenantValidationResult Inactive() => new TenantValidationResult(TenantValidationStatus.Inactive, null);

        /// <summary>Public contract for tenant lookup or access validation.</summary>
        public static TenantValidationResult Unavailable() => new TenantValidationResult(TenantValidationStatus.Unavailable, null);
    }
}
