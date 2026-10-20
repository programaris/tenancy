namespace Programaris.Tenancy.Http
{
    /// <summary>Controls which absolute HTTPS origins may receive outbound requests.</summary>
    public sealed class TenantHttpClientOptions
    {
        public TenantPropagationMode Mode { get; set; } = TenantPropagationMode.Explicit;

        public string HeaderName { get; set; } = "X-Tenant";

        public IList<Uri> AllowedOrigins { get; } = new List<Uri>();
    }
}
