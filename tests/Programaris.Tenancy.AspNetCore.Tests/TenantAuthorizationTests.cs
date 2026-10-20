using System.Net;
using System.Security.Claims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Programaris.Tenancy.Abstractions;

using Xunit;

namespace Programaris.Tenancy.AspNetCore.Tests
{
    public sealed class TenantAuthorizationTests
    {
        [Fact]
        public async Task ValidTenantDoesNotGrantUnauthenticatedAccess()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var request = Request("programaris");
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MissingTenantNeverUsesDefaultTenant()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/secure", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ExplicitlyPublicTenantEndpointStillValidatesExistence()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var validRequest = Request("programaris");
            using var invalidRequest = Request("missing");
            validRequest.RequestUri = new Uri("http://localhost/public");
            invalidRequest.RequestUri = new Uri("http://localhost/public");

            using var valid = await client.SendAsync(validRequest, TestContext.Current.CancellationToken);
            using var invalid = await client.SendAsync(invalidRequest, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, invalid.StatusCode);
        }

        [Fact]
        public async Task StoreOutageFailsClosed()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var request = Request("unavailable");
            request.RequestUri = new Uri("http://localhost/public");

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        [Fact]
        public async Task OptionalEndpointDoesNotInheritAuthorizationContext()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var selected = Request("programaris");
            selected.RequestUri = new Uri("http://localhost/public");

            using var first = await client.SendAsync(selected, TestContext.Current.CancellationToken);
            using var second = await client.GetAsync("/health", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal("none", await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        private static HttpRequestMessage Request(string slug)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/secure");
            request.Headers.Add("X-Tenant", slug);
            return request;
        }

        private static async Task<WebApplication> CreateAppAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddProgramarisTenancy();
            builder.Services.AddProgramarisTenantAuthorization();
            builder.Services.AddSingleton<ITenantStore, StubTenantStore>();
            builder.Services.AddSingleton<ITenantAccessValidator, DenyAccessValidator>();

            WebApplication app = builder.Build();
            app.UseRouting();
            app.UseProgramarisTenancy();
            app.UseProgramarisTenantAuthorization();
            app.MapGet("/secure", (ITenantAuthorizationContextAccessor context) =>
                Results.Text(context.Current?.Slug ?? "none"));
            app.MapGet("/public", (ITenantAuthorizationContextAccessor context) =>
                Results.Text(context.Current?.Slug ?? "none")).AllowUnauthenticatedTenant();
            app.MapGet("/health", (ITenantAuthorizationContextAccessor context) =>
                Results.Text(context.Current?.Slug ?? "none")).AllowMissingTenant();
            await app.StartAsync(TestContext.Current.CancellationToken);
            return app;
        }

        private sealed class StubTenantStore : ITenantStore
        {
            public Task<TenantValidationResult> FindAsync(string slug, CancellationToken cancellationToken = default)
            {
                TenantValidationResult result = slug switch
                {
                    "programaris" => TenantValidationResult.Found(new TenantContext(slug)),
                    "unavailable" => TenantValidationResult.Unavailable(),
                    _ => TenantValidationResult.NotFound()
                };

                return Task.FromResult(result);
            }
        }

        private sealed class DenyAccessValidator : ITenantAccessValidator
        {
            public Task<TenantAccessResult> ValidateAsync(TenantAccessRequest request, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(TenantAccessResult.Denied());
            }
        }
    }
}
