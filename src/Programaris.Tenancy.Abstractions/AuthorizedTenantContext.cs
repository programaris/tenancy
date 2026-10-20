namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Tenant proven active and accessible to this request's authenticated principal.</summary>
    /// <remarks>This does not grant permissions for operations beyond the operation evaluated.</remarks>
    public sealed class AuthorizedTenantContext
    {
        public AuthorizedTenantContext(TenantContext tenant, string? operation)
        {
            Tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            Operation = operation;
        }

        public TenantContext Tenant { get; }

        public string? Operation { get; }
    }
}
