namespace Programaris.Tenancy.Http
{
    /// <summary>Per-client tenant forwarding policy. No destinations are approved by default.</summary>
    public sealed class TenantHttpClientOptions
    {
        /// <summary>Gets or sets the tenant propagation mode.</summary>
        public TenantPropagationMode Mode { get; set; } = TenantPropagationMode.Explicit;

        /// <summary>Gets or sets the outgoing tenant header name.</summary>
        public string HeaderName { get; set; } = "X-Tenant";

        /// <summary>Exact HTTPS origins allowed to receive an outbound request (scheme, host and port).</summary>
        public ICollection<Uri> AllowedOrigins { get; } = new List<Uri>();
    }
}
