namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Tenant proven active and accessible to this request's authenticated principal.</summary>
    /// <remarks>This does not grant permissions for operations beyond the operation evaluated.</remarks>
    public sealed class AuthorizedTenantContext
    {
        /// <summary>Creates the authorized tenant context for a specific request.</summary>
        public AuthorizedTenantContext(TenantContext tenant, string? operation)
        {
            Tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            Operation = operation;
        }

        /// <summary>Gets the trusted tenant that was validated by the store.</summary>
        public TenantContext Tenant { get; }

        /// <summary>Gets the operation against which tenant access was validated.</summary>
        public string? Operation { get; }
    }
}
