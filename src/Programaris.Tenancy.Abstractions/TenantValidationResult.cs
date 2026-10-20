namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Tenant lookup outcome. Only Active results carry a trusted tenant.</summary>
    public sealed class TenantValidationResult
    {
        private TenantValidationResult(TenantValidationStatus status, TenantContext? tenant)
        {
            Status = status;
            Tenant = tenant;
        }

        /// <summary>Gets the operational lookup result.</summary>
        public TenantValidationStatus Status { get; }

        /// <summary>Gets the trusted tenant that was validated by the store.</summary>
        public TenantContext? Tenant { get; }

        /// <summary>Indicates that a tenant exists and is operational.</summary>
        public static TenantValidationResult Active(TenantContext tenant)
        {
            if (tenant is null)
            {
                throw new ArgumentNullException(nameof(tenant));
            }

            return new TenantValidationResult(TenantValidationStatus.Active, tenant);
        }

        /// <summary>Indicates that a tenant is not currently operational.</summary>
        public static TenantValidationResult Inactive() => new TenantValidationResult(TenantValidationStatus.Inactive, null);

        /// <summary>Indicates that no tenant exists with the requested slug.</summary>
        public static TenantValidationResult NotFound() => new TenantValidationResult(TenantValidationStatus.NotFound, null);

        /// <summary>Indicates that a required trusted decision could not be obtained.</summary>
        public static TenantValidationResult Unavailable() => new TenantValidationResult(TenantValidationStatus.Unavailable, null);
    }
}
