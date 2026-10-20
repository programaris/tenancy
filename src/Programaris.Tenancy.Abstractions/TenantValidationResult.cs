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

        public TenantValidationStatus Status { get; }

        public TenantContext? Tenant { get; }

        public static TenantValidationResult Active(TenantContext tenant)
        {
            if (tenant is null)
            {
                throw new ArgumentNullException(nameof(tenant));
            }

            return new TenantValidationResult(TenantValidationStatus.Active, tenant);
        }

        public static TenantValidationResult Inactive() => new TenantValidationResult(TenantValidationStatus.Inactive, null);

        public static TenantValidationResult NotFound() => new TenantValidationResult(TenantValidationStatus.NotFound, null);

        public static TenantValidationResult Unavailable() => new TenantValidationResult(TenantValidationStatus.Unavailable, null);
    }
}
