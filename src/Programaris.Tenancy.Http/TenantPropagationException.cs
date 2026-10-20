namespace Programaris.Tenancy.Http
{
    /// <summary>Raised before the request is dispatched when a tenant propagation rule is violated.</summary>
    public sealed class TenantPropagationException : InvalidOperationException
    {
        /// <summary>Creates an exception for invalid or unsafe HTTP tenant forwarding.</summary>
        public TenantPropagationException(string message)
            : base(message)
        {
        }
    }
}
