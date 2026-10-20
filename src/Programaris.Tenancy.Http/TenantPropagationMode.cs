namespace Programaris.Tenancy.Http
{
    /// <summary>Defines how outbound requests must supply a selected tenant.</summary>
    public enum TenantPropagationMode
    {
        Explicit = 0,
        Required = 1,
        Optional = 2
    }
}
