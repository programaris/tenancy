namespace Programaris.Tenancy.Http
{
    /// <summary>Thrown when outbound tenant propagation does not meet its security policy.</summary>
    public sealed class TenantPropagationException : InvalidOperationException
    {
        public TenantPropagationException(string message) : base(message)
        {
        }
    }
}
