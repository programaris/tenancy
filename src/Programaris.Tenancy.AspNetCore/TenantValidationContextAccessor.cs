using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Request-scoped, single-assignment validated tenant context.</summary>
    public sealed class TenantValidationContextAccessor : ITenantValidationContextAccessor
    {
        /// <summary>Gets the active registry-validated tenant, without membership authorization.</summary>
        public TenantContext? Current { get; private set; }

        internal void Initialize(TenantContext tenant)
        {
            ArgumentNullException.ThrowIfNull(tenant);

            if (Current is not null)
            {
                throw new InvalidOperationException("Tenant validation context has already been initialized.");
            }

            Current = tenant;
        }
    }
}
