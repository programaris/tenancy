using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Validates tenant existence and access using independently registered trusted providers.</summary>
    /// <remarks>
    /// Install after authentication and UseProgramarisTenancy, and before endpoint execution.
    /// AllowMissingTenant marks genuinely public endpoints and bypasses tenant protection entirely.
    /// </remarks>
    public sealed class TenantProtectionMiddleware
    {
        private readonly RequestDelegate _Next;
        private readonly ILogger<TenantProtectionMiddleware> _Logger;

        public TenantProtectionMiddleware(RequestDelegate next, ILogger<TenantProtectionMiddleware> logger)
        {
            _Next = next ?? throw new ArgumentNullException(nameof(next));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(
            HttpContext context,
            ITenantContextAccessor selected,
            TenantAuthorizationContextAccessor authorized,
            ITenantStore store,
            ITenantAccessValidator validator)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.GetEndpoint()?.Metadata.GetMetadata<AllowMissingTenantMetadata>() is not null)
            {
                await _Next(context);

                return;
            }

            TenantContext? tenant = selected.Current;

            if (tenant is null)
            {
                await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "tenant_missing");

                return;
            }

            if (context.User.Identity?.IsAuthenticated != true)
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "tenant_authentication_required");

                return;
            }

            TenantValidationResult lookup;

            try
            {
                lookup = await store.FindAsync(tenant.Slug, context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _Logger.LogWarning(exception, "Tenant store lookup failed.");
                await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_validation_unavailable");

                return;
            }

            if (lookup is null)
            {
                await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_validation_unavailable");

                return;
            }

            switch (lookup.Status)
            {
                case TenantValidationStatus.NotFound:
                    await WriteProblemAsync(context, StatusCodes.Status404NotFound, "tenant_not_found");
                    return;
                case TenantValidationStatus.Inactive:
                    await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "tenant_inactive");
                    return;
                case TenantValidationStatus.Active:
                    if (lookup.Tenant is null ||
                        !string.Equals(lookup.Tenant.Slug, tenant.Slug, StringComparison.Ordinal))
                    {
                        _Logger.LogError("Tenant store returned an inconsistent active tenant.");
                        await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_validation_unavailable");

                        return;
                    }
                    break;
                default:
                    await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_validation_unavailable");
                    return;
            }

            string? operation = context.GetEndpoint()?.Metadata.GetMetadata<TenantOperationMetadata>()?.Operation;
            TenantAccessResult decision;

            try
            {
                decision = await validator.ValidateAsync(
                    new TenantAccessRequest(tenant.Slug, context.User, operation),
                    context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _Logger.LogWarning(exception, "Tenant access validation failed.");
                await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_access_unavailable");

                return;
            }

            if (decision is null || decision.Decision == TenantAccessDecision.Unavailable)
            {
                await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_access_unavailable");

                return;
            }

            if (decision.Decision != TenantAccessDecision.Allowed)
            {
                await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "tenant_access_denied");

                return;
            }

            authorized.Initialize(new AuthorizedTenantContext(lookup.Tenant!, operation));

            await _Next(context);
        }

        private static async Task WriteProblemAsync(HttpContext context, int status, string code)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            string body = JsonSerializer.Serialize(new
            {
                type = "about:blank",
                title = "Tenant access could not be established",
                status,
                code
            });

            await context.Response.WriteAsync(body, context.RequestAborted);
        }
    }
}
