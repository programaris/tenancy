using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Scoped, write-once result of tenant authorization, separate from tenant selection.</summary>
    public sealed class TenantAuthorizationContextAccessor : ITenantAuthorizationContextAccessor
    {
        /// <summary>Gets the authorized context already established for the current scope.</summary>
        public AuthorizedTenantContext? Current { get; private set; }

        /// <summary>Returns the authorized context or throws if not established.</summary>
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
