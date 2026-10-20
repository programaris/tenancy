using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Scoped, single-assignment authorization context.</summary>
    public sealed class TenantAuthorizationContextAccessor : ITenantAuthorizationContextAccessor
    {
        public TenantContext? Current { get; private set; }

        internal void Initialize(TenantContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (Current is not null)
            {
                throw new InvalidOperationException("Tenant authorization context is already initialized.");
            }

            Current = context;
        }
    }
}
