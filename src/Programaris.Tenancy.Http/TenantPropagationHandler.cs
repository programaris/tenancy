using System.Net.Http;

using Microsoft.Extensions.Options;

namespace Programaris.Tenancy.Http
{
    /// <summary>Enforces exact HTTPS origins and request-bound tenant injection.</summary>
    public sealed class TenantPropagationHandler : DelegatingHandler
    {
        private readonly TenantPropagationMode _Mode;
        private readonly string _HeaderName;
        private readonly HashSet<string> _AllowedOrigins;

        /// <summary>Validates and snapshots the outbound forwarding policy.</summary>
        public TenantPropagationHandler(IOptions<TenantHttpClientOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            TenantHttpClientOptions policy = options.Value;

            if (!Enum.IsDefined(policy.Mode))
            {
                throw new ArgumentException("Invalid tenant propagation mode.", nameof(options));
            }

            if (!TenantHttpRequestExtensions.IsValidHeaderName(policy.HeaderName))
            {
                throw new ArgumentException("Invalid tenant header name.", nameof(options));
            }

            _Mode = policy.Mode;
            _HeaderName = policy.HeaderName;
            _AllowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Uri origin in policy.AllowedOrigins)
            {
                if (origin is null ||
                    !origin.IsAbsoluteUri ||
                    origin.Scheme != Uri.UriSchemeHttps ||
                    origin.AbsolutePath != "/" ||
                    !string.IsNullOrEmpty(origin.UserInfo) ||
                    !string.IsNullOrEmpty(origin.Query) ||
                    !string.IsNullOrEmpty(origin.Fragment))
                {
                    throw new ArgumentException("Only exact HTTPS origins without paths or credentials are allowed.", nameof(options));
                }

                _AllowedOrigins.Add(origin.GetLeftPart(UriPartial.Authority));
            }

            if (_AllowedOrigins.Count == 0)
            {
                throw new ArgumentException("At least one allowed HTTPS origin is required.", nameof(options));
            }
        }

        /// <summary>Checks destination and selection before the underlying network handler executes.</summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            Uri? destination = request.RequestUri;

            if (destination is null ||
                !destination.IsAbsoluteUri ||
                destination.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(destination.UserInfo) ||
                !_AllowedOrigins.Contains(destination.GetLeftPart(UriPartial.Authority)))
            {
                throw new TenantPropagationException("The destination is not on the exact HTTPS origin allowlist.");
            }

            if (request.Headers.Contains(_HeaderName))
            {
                throw new TenantPropagationException("A tenant header already exists; use per-request selection instead.");
            }

            bool hasSelection = request.Options.TryGetValue(TenantHttpRequestExtensions.SelectionKey, out TenantRequestSelection? selection);

            if (!hasSelection || selection is null)
            {
                if (_Mode == TenantPropagationMode.Required)
                {
                    throw new TenantPropagationException("Tenant selection is required for this outbound request.");
                }

                return base.SendAsync(request, cancellationToken);
            }

            if (!string.Equals(selection.HeaderName, _HeaderName, StringComparison.OrdinalIgnoreCase))
            {
                throw new TenantPropagationException("The request tenant header name conflicts with the client policy.");
            }

            if (_Mode == TenantPropagationMode.Explicit && selection.FromContext)
            {
                throw new TenantPropagationException("Scoped capture is not enabled in Explicit mode.");
            }

            if (!TenantSlug.TryParse(selection.Slug, out string canonical))
            {
                throw new TenantPropagationException("Invalid tenant selection.");
            }

            request.Headers.Add(_HeaderName, canonical);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
