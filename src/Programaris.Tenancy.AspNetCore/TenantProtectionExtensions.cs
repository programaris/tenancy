using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Opt-in access enforcement; providers are registered by the consuming application.</summary>
    public static class TenantProtectionExtensions
    {
        /// <summary>Registers the scoped authorized context; does not register a permissive store or validator.</summary>
        public static IServiceCollection AddProgramarisTenantProtection(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddScoped<TenantAuthorizationContextAccessor>();
            services.AddScoped<ITenantAuthorizationContextAccessor>(provider => provider.GetRequiredService<TenantAuthorizationContextAccessor>());

            return services;
        }

        /// <summary>Checks tenant existence and caller access after authentication and tenant resolution.</summary>
        public static IApplicationBuilder UseProgramarisTenantProtection(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            return app.UseMiddleware<TenantProtectionMiddleware>();
        }

        /// <summary>Defines an optional operation scope for the tenant access validator.</summary>
        public static TBuilder RequireProgramarisTenantOperation<TBuilder>(this TBuilder builder, string operation)
            where TBuilder : IEndpointConventionBuilder
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.WithMetadata(new TenantOperationMetadata(operation));

            return builder;
        }
    }
}
