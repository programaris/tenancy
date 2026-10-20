using System.Text.Json;

using Microsoft.AspNetCore.Http;

using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Validates selected tenant and principal access using independently provided trust sources.</summary>
    public sealed class TenantAuthorizationMiddleware
    {
        private readonly RequestDelegate _Next;

        public TenantAuthorizationMiddleware(RequestDelegate next)
        {
            _Next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(
            HttpContext context,
            ITenantContextAccessor selection,
            ITenantStore store,
            ITenantAccessValidator validator,
            TenantAuthorizationContextAccessor authorized)
        {
            ArgumentNullException.ThrowIfNull(context);

            TenantContext? requested = selection.Current;
            bool optional = context.GetEndpoint()?.Metadata.GetMetadata<AllowMissingTenantMetadata>() is not null;

            if (requested is null)
            {
                if (optional)
                {
                    await _Next(context);
                    return;
                }

                await ProblemAsync(context, StatusCodes.Status400BadRequest, "tenant_header_missing");
                return;
            }

            TenantValidationResult result;

            try
            {
                result = await store.FindAsync(requested.Slug, context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                await ProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_registry_unavailable");
                return;
            }

            if (result is null || result.Status == TenantValidationStatus.Unavailable)
            {
                await ProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_registry_unavailable");
                return;
            }

            if (!result.IsValid || result.Tenant is null ||
                !string.Equals(result.Tenant.Slug, requested.Slug, StringComparison.Ordinal))
            {
                await ProblemAsync(context, StatusCodes.Status404NotFound, "tenant_not_found");
                return;
            }

            bool publicTenant = context.GetEndpoint()?.Metadata.GetMetadata<AllowUnauthenticatedTenantMetadata>() is not null;

            if (!publicTenant)
            {
                if (context.User.Identity?.IsAuthenticated != true)
                {
                    await ProblemAsync(context, StatusCodes.Status401Unauthorized, "tenant_authentication_required");
                    return;
                }

                TenantAccessResult access;

                try
                {
                    access = await validator.ValidateAsync(
                        new TenantAccessRequest(requested.Slug, context.User),
                        context.RequestAborted);
                }
                catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    await ProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_authorization_unavailable");
                    return;
                }

                if (access is null || access.Status == TenantAccessStatus.Unavailable)
                {
                    await ProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "tenant_authorization_unavailable");
                    return;
                }

                if (!access.IsGranted)
                {
                    int status = access.Status == TenantAccessStatus.Unauthenticated
                        ? StatusCodes.Status401Unauthorized
                        : StatusCodes.Status403Forbidden;
                    await ProblemAsync(context, status, "tenant_access_denied");
                    return;
                }
            }

            authorized.Initialize(result.Tenant);
            await _Next(context);
        }

        private static Task ProblemAsync(HttpContext context, int status, string code)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            return context.Response.WriteAsync(
                JsonSerializer.Serialize(new { type = "about:blank", title = code, status, code }),
                context.RequestAborted);
        }
    }
}
