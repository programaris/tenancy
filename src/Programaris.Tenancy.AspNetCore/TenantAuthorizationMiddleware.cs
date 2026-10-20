using Microsoft.AspNetCore.Http;

using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Validates a selected tenant against trusted sources, then enforces principal access when required.</summary>
    public sealed class TenantAuthorizationMiddleware
    {
        private readonly RequestDelegate _Next;

        /// <summary>Creates authorization middleware.</summary>
        public TenantAuthorizationMiddleware(RequestDelegate next)
        {
            _Next = next ?? throw new ArgumentNullException(nameof(next));
        }

        /// <summary>
        /// For protected endpoints, authentication is verified before tenant existence is queried.
        /// Public tenant endpoints only obtain validated context, never authorized context.
        /// </summary>
        public async Task InvokeAsync(
            HttpContext context,
            ITenantContextAccessor selection,
            ITenantStore store,
            ITenantAccessValidator validator,
            TenantValidationContextAccessor validated,
            TenantAuthorizationContextAccessor authorized,
            ITenantFailureResponseWriter failures)
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

                await failures.WriteAsync(context, TenantFailureKind.MissingTenant);
                return;
            }

            bool publicTenant = context.GetEndpoint()?.Metadata.GetMetadata<AllowUnauthenticatedTenantMetadata>() is not null;

            if (!publicTenant && context.User.Identity?.IsAuthenticated != true)
            {
                await failures.WriteAsync(context, TenantFailureKind.AuthenticationRequired);
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
                await failures.WriteAsync(context, TenantFailureKind.RegistryUnavailable);
                return;
            }

            if (result is null || result.Status == TenantValidationStatus.Unavailable)
            {
                await failures.WriteAsync(context, TenantFailureKind.RegistryUnavailable);
                return;
            }

            if (!result.IsValid || result.Tenant is null ||
                !string.Equals(result.Tenant.Slug, requested.Slug, StringComparison.Ordinal))
            {
                await failures.WriteAsync(context, TenantFailureKind.NotFoundOrInactive);
                return;
            }

            if (result.Tenant.TenantId == Guid.Empty)
            {
                await failures.WriteAsync(context, TenantFailureKind.RegistryUnavailable);
                return;
            }

            validated.Initialize(result.Tenant);

            if (publicTenant)
            {
                await _Next(context);
                return;
            }

            TenantAccessResult access;

            try
            {
                access = await validator.ValidateAsync(
                    new TenantAccessRequest(
                        requested.Slug,
                        context.User,
                        context.GetEndpoint()?.Metadata.GetMetadata<TenantOperationMetadata>()?.Operation,
                        result.Tenant.TenantId),
                    context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                await failures.WriteAsync(context, TenantFailureKind.AuthorizationUnavailable);
                return;
            }

            if (access is null || access.Status == TenantAccessStatus.Unavailable)
            {
                await failures.WriteAsync(context, TenantFailureKind.AuthorizationUnavailable);
                return;
            }

            if (!access.IsGranted)
            {
                await failures.WriteAsync(
                    context,
                    access.Status == TenantAccessStatus.Unauthenticated
                        ? TenantFailureKind.AuthenticationRequired
                        : TenantFailureKind.AccessDenied);
                return;
            }

            authorized.Initialize(result.Tenant);
            await _Next(context);
        }
    }
}
