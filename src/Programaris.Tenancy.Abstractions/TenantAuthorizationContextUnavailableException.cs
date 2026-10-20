namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Thrown when code requires a tenant authorization context that was never established.</summary>
    public sealed class TenantAuthorizationContextUnavailableException : InvalidOperationException
    {
        public TenantAuthorizationContextUnavailableException()
            : base("An authorized tenant context is required but unavailable.")
        {
        }
    }
}
