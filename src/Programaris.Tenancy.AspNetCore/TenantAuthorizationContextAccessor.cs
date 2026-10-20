using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Scoped, write-once result of tenant authorization, separate from tenant selection.</summary>
    public sealed class TenantAuthorizationContextAccessor : ITenantAuthorizationContextAccessor
    {
        public AuthorizedTenantContext? Current { get; private set; }

        public AuthorizedTenantContext GetRequired()
        {
            return Current ?? throw new TenantAuthorizationContextUnavailableException();
        }

        internal void Initialize(AuthorizedTenantContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (Current is not null)
            {
                throw new InvalidOperationException("Tenant authorization was already initialized.");
            }

            Current = context;
        }
    }
}
