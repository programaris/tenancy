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

        [Fact]
        public async Task AuthenticatedMemberIsGrantedOnlyForItsTenant()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var authorized = Request("programaris");
            authorized.Headers.Add("X-Test-Principal", "alice");
            using var granted = await client.SendAsync(authorized, TestContext.Current.CancellationToken);

            using var deniedRequest = Request("programaris");
            deniedRequest.Headers.Add("X-Test-Principal", "bob");
            using var denied = await client.SendAsync(deniedRequest, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
            Assert.Equal("programaris", await granted.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }

        [Fact]
        public async Task EndpointOperationIsPassedToTrustedValidator()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();

            using var allowedRequest = Request("programaris");
            allowedRequest.RequestUri = new Uri("http://localhost/secure-operation");
            allowedRequest.Headers.Add("X-Test-Principal", "alice");
            using var allowed = await client.SendAsync(allowedRequest, TestContext.Current.CancellationToken);

            using var deniedRequest = Request("programaris");
            deniedRequest.RequestUri = new Uri("http://localhost/secure-operation-denied");
            deniedRequest.Headers.Add("X-Test-Principal", "alice");
            using var denied = await client.SendAsync(deniedRequest, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }

        [Fact]
        public async Task AuthorizationProviderUnavailableFailsClosed()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var request = Request("access-unavailable");
            request.Headers.Add("X-Test-Principal", "alice");
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        [Fact]
        public async Task InactiveOrMismatchedStoreRecordsAreRejected()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();

            foreach (string slug in new[] { "inactive", "mismatch" })
            {
                using var request = Request(slug);
                request.RequestUri = new Uri("http://localhost/public");
                using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async Task ConcurrentAuthorizedAndDeniedRequestsAreIsolated()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();

            Task[] requests = Enumerable.Range(0, 64).Select(async i =>
            {
                bool allowed = i % 2 == 0;
                using var request = Request("programaris");
                request.Headers.Add("X-Test-Principal", allowed ? "alice" : "bob");
                using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
                Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
            }).ToArray();

            await Task.WhenAll(requests);
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
            // Test-only authenticated principal injection; production consumers must use ASP.NET Core authentication.
            app.Use(async (context, next) =>
            {
                if (context.Request.Headers.TryGetValue("X-Test-Principal", out var principalName))
                {
                    var identity = new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.Name, principalName.ToString()) },
                        "Test");
                    context.User = new ClaimsPrincipal(identity);
                }

                await next(context);
            });
            app.UseProgramarisTenantAuthorization();
            app.MapGet("/secure", (ITenantAuthorizationContextAccessor context) =>
                Results.Text(context.Current?.Slug ?? "none"));
            app.MapGet("/secure-operation", () => Results.Ok())
                .RequireProgramarisTenantOperation("profile.read");
            app.MapGet("/secure-operation-denied", () => Results.Ok())
                .RequireProgramarisTenantOperation("profile.delete");
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
                    "access-unavailable" => TenantValidationResult.Found(new TenantContext(slug)),
                    "inactive" => TenantValidationResult.Inactive(),
                    "mismatch" => TenantValidationResult.Found(new TenantContext("programaris")),
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
                if (request.TenantSlug == "access-unavailable")
                {
                    return Task.FromResult(TenantAccessResult.Unavailable());
                }

                return Task.FromResult(
                    request.TenantSlug == "programaris" &&
                    request.Principal.Identity?.Name == "alice" &&
                    (request.Operation is null || request.Operation == "profile.read")
                        ? TenantAccessResult.Granted()
                        : TenantAccessResult.Denied());
            }
        }
    }
}
