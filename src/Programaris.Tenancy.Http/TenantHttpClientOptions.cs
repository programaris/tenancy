namespace Programaris.Tenancy.Http
{
    /// <summary>Controls which absolute HTTPS origins may receive outbound requests.</summary>
    public sealed class TenantHttpClientOptions
    {
        /// <summary>Configures or provides tenant-aware HTTP integration.</summary>
        public TenantPropagationMode Mode { get; set; } = TenantPropagationMode.Explicit;

        /// <summary>Configures or provides tenant-aware HTTP integration.</summary>
        public string HeaderName { get; set; } = "X-Tenant";

        /// <summary>Configures or provides tenant-aware HTTP integration.</summary>
        public IList<Uri> AllowedOrigins { get; } = new List<Uri>();
    }
}
