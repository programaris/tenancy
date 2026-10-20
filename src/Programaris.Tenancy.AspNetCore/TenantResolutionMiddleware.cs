using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Reads untrusted tenant selection from the header and explicitly enabled host sources.</summary>
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

        /// <summary>Establishes at most one selected tenant, scoped to the current request.</summary>
        public async Task InvokeAsync(HttpContext context, TenantContextAccessor accessor)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(accessor);

            TenantHeaderOptions options = _Options.Value;
            bool allowMissing = context.GetEndpoint()?.Metadata.GetMetadata<AllowMissingTenantMetadata>() is not null;
            string? selected = null;

            if (context.Request.Headers.TryGetValue(options.HeaderName, out var values))
            {
                if (values.Count != 1 || !TenantSlug.TryParse(values[0], out string slug))
                {
                    await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "tenant_header_invalid");
                    return;
                }

                selected = slug;
            }

            var metadata = context.GetEndpoint()?.Metadata.GetOrderedMetadata<TenantSelectionSourceMetadata>();

            if (metadata is not null)
            {
                var seen = new HashSet<Type>();

                foreach (TenantSelectionSourceMetadata entry in metadata)
                {
                    if (!seen.Add(entry.SourceType))
                    {
                        await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "tenant_selection_source_duplicate");
                        return;
                    }

                    ITenantSelectionSource? source =
                        context.RequestServices.GetService(entry.SourceType) as ITenantSelectionSource;

                    if (source is null)
                    {
                        await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_selection_source_unavailable");
                        return;
                    }

                    IReadOnlyList<string>? candidates;

                    try
                    {
                        candidates = await source.GetCandidatesAsync(context, context.RequestAborted);
                    }
                    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_selection_source_unavailable");
                        return;
                    }

                    if (candidates is null || candidates.Count > 1)
                    {
                        await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "tenant_selection_invalid");
                        return;
                    }

                    if (candidates.Count == 0)
                    {
                        continue;
                    }

                    if (!TenantSlug.TryParse(candidates[0], out string candidate))
                    {
                        await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "tenant_selection_invalid");
                        return;
                    }

                    if (selected is not null && !string.Equals(selected, candidate, StringComparison.Ordinal))
                    {
                        await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "tenant_selection_conflict");
                        return;
                    }

                    selected = candidate;
                }
            }

            if (selected is null)
            {
                if (!allowMissing)
                {
                    await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "tenant_header_missing");
                    return;
                }

                await _Next(context);
                return;
            }

            accessor.Initialize(new TenantContext(selected));
            await _Next(context);
        }

        private static Task WriteProblemAsync(HttpContext context, int status, string code)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(new { type = "about:blank", title = code, status, code }),
                context.RequestAborted);
        }
    }
}
