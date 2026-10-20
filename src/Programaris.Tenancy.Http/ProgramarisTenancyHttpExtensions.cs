using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Programaris.Tenancy.Http
{
    /// <summary>Registers tenant-aware outbound requests with an origin allowlist and redirects disabled.</summary>
    public static class ProgramarisTenancyHttpExtensions
    {
        public static IHttpClientBuilder AddProgramarisTenancyHttp(
            this IHttpClientBuilder builder,
            Action<TenantHttpClientOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            builder.Services.Configure(builder.Name, configure);
            builder.Services.AddTransient<TenantPropagationHandler>();

            // HttpClientHandler redirects happen below DelegatingHandler and bypass destination checks.
            // This must be the final primary handler configuration on this named client.
            builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

            builder.AddHttpMessageHandler(provider =>
            {
                IOptionsMonitor<TenantHttpClientOptions> monitor =
                    provider.GetRequiredService<IOptionsMonitor<TenantHttpClientOptions>>();
                return new TenantPropagationHandler(
                    Options.Create(monitor.Get(builder.Name)));
            });

            return builder;
        }
    }
}
