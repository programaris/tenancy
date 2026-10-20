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

        public TenantValidationStatus Status { get; }

        public TenantContext? Tenant { get; }

        public bool IsValid => Status == TenantValidationStatus.Found;

        public static TenantValidationResult Found(TenantContext tenant)
        {
            if (tenant is null)
            {
                throw new ArgumentNullException(nameof(tenant));
            }

            return new TenantValidationResult(TenantValidationStatus.Found, tenant);
        }

        public static TenantValidationResult NotFound() => new TenantValidationResult(TenantValidationStatus.NotFound, null);

        public static TenantValidationResult Inactive() => new TenantValidationResult(TenantValidationStatus.Inactive, null);

        public static TenantValidationResult Unavailable() => new TenantValidationResult(TenantValidationStatus.Unavailable, null);
    }
}
