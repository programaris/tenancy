using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Opt-in tenant registry validation and authenticated access enforcement.</summary>
    public static class ProgramarisTenantAuthorizationExtensions
    {
        /// <summary>Registers scoped authorized context; consumers must register ITenantStore and ITenantAccessValidator.</summary>
        public static IServiceCollection AddProgramarisTenantAuthorization(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddScoped<TenantValidationContextAccessor>();
            services.TryAddScoped<ITenantValidationContextAccessor>(
                provider => provider.GetRequiredService<TenantValidationContextAccessor>());
            services.TryAddSingleton<ITenantFailureResponseWriter, DefaultTenantFailureResponseWriter>();
            services.TryAddScoped<TenantAuthorizationContextAccessor>();
            services.TryAddScoped<ITenantAuthorizationContextAccessor>(
                provider => provider.GetRequiredService<TenantAuthorizationContextAccessor>());
            return services;
        }

        /// <summary>Execute after routing, tenant resolution and authentication, before endpoint execution.</summary>
        public static IApplicationBuilder UseProgramarisTenantAuthorization(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);
            return app.UseMiddleware<TenantAuthorizationMiddleware>();
        }

        /// <summary>Annotates the operation to be checked by the trusted tenant access validator.</summary>
        public static TBuilder RequireProgramarisTenantOperation<TBuilder>(this TBuilder builder, string operation)
            where TBuilder : IEndpointConventionBuilder
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.WithMetadata(new TenantOperationMetadata(operation));
            return builder;
        }

        /// <summary>Allows a validated tenant to be accessed without membership checks, for explicitly public endpoints only.</summary>
        public static TBuilder AllowUnauthenticatedTenant<TBuilder>(this TBuilder builder)
            where TBuilder : IEndpointConventionBuilder
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.WithMetadata(new AllowUnauthenticatedTenantMetadata());
            return builder;
        }
    }
}
