namespace Programaris.Tenancy.Http
{
    /// <summary>Raised before the request is dispatched when a tenant propagation rule is violated.</summary>
    public sealed class TenantPropagationException : InvalidOperationException
    {
        public TenantPropagationException(string message)
            : base(message)
        {
        }
    }
}
