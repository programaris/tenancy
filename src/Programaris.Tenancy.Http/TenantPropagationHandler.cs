using Programaris.Tenancy.Http.Internal;

namespace Programaris.Tenancy.Http
{
    /// <summary>Performs allowlisted tenant propagation without accessing a pooled handler's DI scope.</summary>
    public sealed class TenantPropagationHandler : DelegatingHandler
    {
        private readonly TenantPropagationMode _Mode;
        private readonly string _HeaderName;
        private readonly HashSet<string> _AllowedOrigins;

        /// <summary>Builds a reusable HTTP handler with an immutable destination policy.</summary>
        public TenantPropagationHandler(TenantHttpClientOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (!Enum.IsDefined(options.Mode))
            {
                throw new ArgumentException("Invalid tenant propagation mode.", nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.HeaderName) ||
                !options.HeaderName.All(character =>
                    (character >= 'a' && character <= 'z') ||
                    (character >= 'A' && character <= 'Z') ||
                    (character >= '0' && character <= '9') ||
                    character == '-'))
            {
                throw new ArgumentException("Invalid tenant header name.", nameof(options));
            }

            _Mode = options.Mode;
            _HeaderName = options.HeaderName;
            _AllowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Uri origin in options.AllowedOrigins)
            {
                if (origin is null ||
                    !origin.IsAbsoluteUri ||
                    origin.Scheme != Uri.UriSchemeHttps ||
                    !string.IsNullOrEmpty(origin.UserInfo) ||
                    !string.IsNullOrEmpty(origin.Query) ||
                    !string.IsNullOrEmpty(origin.Fragment) ||
                    origin.AbsolutePath != "/")
                {
                    throw new ArgumentException("Only absolute HTTPS origins without paths, user info, queries or fragments are allowed.", nameof(options));
                }

                _AllowedOrigins.Add(origin.GetLeftPart(UriPartial.Authority));
            }

            if (_AllowedOrigins.Count == 0)
            {
                throw new ArgumentException("At least one allowed HTTPS origin is required.", nameof(options));
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            Uri? destination = request.RequestUri;

            if (destination is null ||
                !destination.IsAbsoluteUri ||
                destination.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(destination.UserInfo) ||
                !_AllowedOrigins.Contains(destination.GetLeftPart(UriPartial.Authority)))
            {
                throw new TenantPropagationException("The destination is not on this HTTP client's exact HTTPS origin allowlist.");
            }

            if (request.Headers.Contains(_HeaderName))
            {
                throw new TenantPropagationException("The request already contains a tenant header.");
            }

            bool hasSelection = request.Options.TryGetValue(TenantRequestOptionsKeys.Selection, out TenantRequestSelection? selection);

            if (!hasSelection || selection is null)
            {
                if (_Mode == TenantPropagationMode.Required)
                {
                    throw new TenantPropagationException("The downstream client requires an explicit or captured tenant.");
                }

                return base.SendAsync(request, cancellationToken);
            }

            if (_Mode == TenantPropagationMode.Explicit && selection.FromContext)
            {
                throw new TenantPropagationException("Context capture is not enabled in Explicit mode.");
            }

            if (!TenantSlug.TryParse(selection.Slug, out string canonical))
            {
                throw new TenantPropagationException("The request tenant is not a canonical slug.");
            }

            request.Headers.Add(_HeaderName, canonical);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
