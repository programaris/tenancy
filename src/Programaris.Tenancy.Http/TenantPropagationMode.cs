namespace Programaris.Tenancy.Http
{
    /// <summary>Defines how outbound requests must supply a selected tenant.</summary>
    public enum TenantPropagationMode
    {
        /// <summary>Available tenant propagation mode.</summary>
        Explicit = 0,
        /// <summary>Available tenant propagation mode.</summary>
        Required = 1,
        /// <summary>Available tenant propagation mode.</summary>
        Optional = 2
    }
}
