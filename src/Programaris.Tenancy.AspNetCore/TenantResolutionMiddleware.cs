using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Reads the untrusted tenant selection header into a request-scoped accessor.</summary>
    public sealed class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _Next;
        private readonly IOptions<TenantHeaderOptions> _Options;

        /// <summary>Creates the tenant selection middleware.</summary>
        public TenantResolutionMiddleware(RequestDelegate next, IOptions<TenantHeaderOptions> options)
        {
            _Next = next ?? throw new ArgumentNullException(nameof(next));
            _Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>Establishes a selected tenant for the current request, when available.</summary>
        public async Task InvokeAsync(HttpContext context, TenantContextAccessor accessor)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(accessor);

            TenantHeaderOptions options = _Options.Value;
            bool allowMissing = context.GetEndpoint()?.Metadata.GetMetadata<AllowMissingTenantMetadata>() is not null;

            if (!context.Request.Headers.TryGetValue(options.HeaderName, out var values) || values.Count == 0)
            {
                if (options.RequireTenant && !allowMissing)
                {
                    await WriteProblemAsync(context, "Tenant header is required", "tenant_header_missing");

                    return;
                }

                await _Next(context);

                return;
            }

            if (values.Count != 1 || !TenantSlug.TryParse(values[0], out string slug))
            {
                await WriteProblemAsync(context, "Invalid tenant header", "tenant_header_invalid");

                return;
            }

            accessor.Initialize(new TenantContext(slug));

            await _Next(context);
        }

        private static async Task WriteProblemAsync(HttpContext context, string title, string code)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            string body = JsonSerializer.Serialize(new
            {
                type = "about:blank",
                title,
                status = StatusCodes.Status400BadRequest,
                code
            });

            await context.Response.WriteAsync(body, context.RequestAborted);
        }
    }
}
