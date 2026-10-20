using Microsoft.Extensions.DependencyInjection;

namespace Programaris.Tenancy.Http
{
    /// <summary>Opt-in registration for approved downstream HTTP clients.</summary>
    public static class ProgramarisTenancyHttpExtensions
    {
        /// <summary>
        /// Adds secure tenant forwarding to one named/typed client.
        /// Disables automatic redirects and shared cookies on supported primary handlers.
        /// </summary>
        public static IHttpClientBuilder AddProgramarisTenancyHttp(
            this IHttpClientBuilder builder,
            Action<TenantHttpClientOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            var options = new TenantHttpClientOptions();
            configure(options);

            // Validate policy at registration rather than deferring errors until the first request.
            _ = new TenantPropagationHandler(options);

            builder.ConfigurePrimaryHttpMessageHandler((handler, _) =>
            {
                switch (handler)
                {
                    case HttpClientHandler httpClientHandler:
                        httpClientHandler.AllowAutoRedirect = false;
                        httpClientHandler.UseCookies = false;
                        break;
                    case SocketsHttpHandler socketsHttpHandler:
                        socketsHttpHandler.AllowAutoRedirect = false;
                        socketsHttpHandler.UseCookies = false;
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Tenant propagation requires HttpClientHandler or SocketsHttpHandler to enforce redirect and cookie safety.");
                }
            });

            builder.AddHttpMessageHandler(() => new TenantPropagationHandler(options));

            return builder;
        }
    }
}
