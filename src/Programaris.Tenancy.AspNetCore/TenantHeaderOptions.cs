namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Configures tenant selection from incoming HTTP requests.</summary>
    public sealed class TenantHeaderOptions
    {
        /// <summary>The default external tenant selection header.</summary>
        public const string DefaultHeaderName = "X-Tenant";

        /// <summary>Name of the request header that carries a tenant slug.</summary>
        public string HeaderName { get; set; } = DefaultHeaderName;

        /// <summary>Whether routes must select a tenant unless marked as optional.</summary>
        public bool RequireTenant { get; set; } = true;
    }
}