namespace Programaris.Tenancy.Http
{
    /// <summary>Thrown when outbound tenant propagation does not meet its security policy.</summary>
    public sealed class TenantPropagationException : InvalidOperationException
    {
        /// <summary>Configures or provides tenant-aware HTTP integration.</summary>
        public TenantPropagationException(string message) : base(message)
        {
        }
    }
}
