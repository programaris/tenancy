using System.Net.Http;

using Microsoft.Extensions.Options;

namespace Programaris.Tenancy.Http
{
    /// <summary>Restricts tenant propagation to approved HTTPS origins without sharing request-scoped tenant state.</summary>
    public sealed class TenantPropagationHandler : DelegatingHandler
    {
        private readonly TenantHttpClientOptions _Options;

        /// <summary>Configures or provides tenant-aware HTTP integration.</summary>
        public TenantPropagationHandler(IOptions<TenantHttpClientOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _Options = options.Value;
        }

        /// <summary>Checks outbound destination and tenant header before forwarding the request.</summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            Uri? destination = request.RequestUri;
            if (destination is null || !destination.IsAbsoluteUri || destination.Scheme != Uri.UriSchemeHttps)
            {
                throw new TenantPropagationException("A permitted absolute HTTPS destination is required.");
            }

            if (!_Options.AllowedOrigins.Any(origin =>
                    origin.IsAbsoluteUri &&
                    origin.Scheme == Uri.UriSchemeHttps &&
                    string.Equals(origin.IdnHost, destination.IdnHost, StringComparison.OrdinalIgnoreCase) &&
                    origin.Port == destination.Port &&
                    origin.AbsolutePath == "/" &&
                    string.IsNullOrEmpty(origin.Query) &&
                    string.IsNullOrEmpty(origin.Fragment) &&
                    string.IsNullOrEmpty(origin.UserInfo)))
            {
                throw new TenantPropagationException("Outbound destination is not in the approved origin allowlist.");
            }

            if (!TenantHttpRequestExtensions.IsValidHeaderName(_Options.HeaderName))
            {
                throw new TenantPropagationException("Invalid tenant header name.");
            }

            if (request.Headers.TryGetValues(_Options.HeaderName, out IEnumerable<string>? values))
            {
                string[] slugs = values.ToArray();
                if (slugs.Length != 1 || !TenantSlug.TryParse(slugs[0], out _))
                {
                    throw new TenantPropagationException("Invalid or ambiguous outbound tenant header.");
                }
            }
            else if (_Options.Mode == TenantPropagationMode.Required)
            {
                throw new TenantPropagationException("Tenant is required for this outbound request.");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
