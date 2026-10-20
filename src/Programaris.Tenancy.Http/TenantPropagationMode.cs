namespace Programaris.Tenancy.Http
{
    /// <summary>Controls whether captured or explicit tenant selections may be propagated.</summary>
    public enum TenantPropagationMode
    {
        /// <summary>Only explicitly supplied tenants are sent. Absent values are not inferred.</summary>
        Explicit = 0,

        /// <summary>A supplied tenant is mandatory, whether explicit or captured from a scoped accessor.</summary>
        Required = 1,

        /// <summary>Explicit or captured tenants are sent; otherwise no tenant is sent.</summary>
        Optional = 2
    }
}
