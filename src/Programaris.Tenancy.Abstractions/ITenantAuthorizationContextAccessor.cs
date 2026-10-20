namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Exposes an authorization decision established during the current request only.</summary>
    public interface ITenantAuthorizationContextAccessor
    {
        AuthorizedTenantContext? Current { get; }

        AuthorizedTenantContext GetRequired();
    }
}
