using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Dependency injection and routing integration for tenant selection.</summary>
    public static class ProgramarisTenancyExtensions
    {
        /// <summary>Adds a request-scoped tenant accessor and validated header options.</summary>
        public static IServiceCollection AddProgramarisTenancy(this IServiceCollection services, Action<TenantHeaderOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            services
                .AddOptions<TenantHeaderOptions>()
                .Configure(options => configure?.Invoke(options))
                .Validate(options => TenantHeaderName.IsValid(options.HeaderName), "The tenant header name must contain only ASCII letters, digits and hyphens.")
                .ValidateOnStart();

            services.AddScoped<TenantContextAccessor>();
            services.AddScoped<ITenantContextAccessor>(provider => provider.GetRequiredService<TenantContextAccessor>());

            return services;
        }

        /// <summary>Adds tenant middleware after routing and before endpoint execution.</summary>
        public static IApplicationBuilder UseProgramarisTenancy(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            return app.UseMiddleware<TenantResolutionMiddleware>();
        }

        /// <summary>
        /// Enables a host-provided selection source on only this endpoint.
        /// The source must be registered as its concrete type in request DI.
        /// </summary>
        public static TBuilder UseProgramarisTenantSelectionSource<TSource, TBuilder>(this TBuilder builder)
            where TSource : class, ITenantSelectionSource
            where TBuilder : IEndpointConventionBuilder
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.WithMetadata(new TenantSelectionSourceMetadata(typeof(TSource)));
            return builder;
        }

        /// <summary>Allows an endpoint to execute without a selected tenant.</summary>
        public static TBuilder AllowMissingTenant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.WithMetadata(new AllowMissingTenantMetadata());

            return builder;
        }
    }
}