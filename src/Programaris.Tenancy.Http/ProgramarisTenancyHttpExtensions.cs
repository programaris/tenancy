using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Programaris.Tenancy.Http
{
    /// <summary>Registers approved tenant-aware outgoing HTTP clients.</summary>
    public static class ProgramarisTenancyHttpExtensions
    {
        /// <summary>Validates HTTPS destinations and disables automatic redirects and cookies.</summary>
        public static IHttpClientBuilder AddProgramarisTenancyHttp(
            this IHttpClientBuilder builder,
            Action<TenantHttpClientOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            var options = new TenantHttpClientOptions();
            configure(options);

            // Refuse unsafe configuration during registration, not on the first network request.
            _ = new TenantPropagationHandler(Options.Create(options));

            // Redirects happen below delegating handlers; cookies must not leak between tenants.
            // Do not override the primary handler after configuring this client.
            builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            });

            builder.AddHttpMessageHandler(() => new TenantPropagationHandler(Options.Create(options)));
            return builder;
        }
    }
}
