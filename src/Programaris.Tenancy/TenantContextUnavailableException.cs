namespace Programaris.Tenancy
{
    /// <summary>Raised when application code requires a tenant but none is selected.</summary>
    public sealed class TenantContextUnavailableException : InvalidOperationException
    {
        /// <summary>Creates a missing-context exception.</summary>
        public TenantContextUnavailableException()
            : base("No tenant context is available for the current operation.")
        {
        }
    }
}