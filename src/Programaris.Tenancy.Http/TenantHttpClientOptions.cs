namespace Programaris.Tenancy.Http
{
    /// <summary>Per-client tenant forwarding policy. No destinations are approved by default.</summary>
    public sealed class TenantHttpClientOptions
    {
        public TenantPropagationMode Mode { get; set; } = TenantPropagationMode.Explicit;

        public string HeaderName { get; set; } = "X-Tenant";

        /// <summary>Exact HTTPS origins allowed to receive an outbound request (scheme, host and port).</summary>
        public ICollection<Uri> AllowedOrigins { get; } = new List<Uri>();
    }
}
