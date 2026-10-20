using System.Net;
using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Programaris.Tenancy.Abstractions;

using Xunit;

namespace Programaris.Tenancy.AspNetCore.Tests
{
    public sealed class TenantProtectionTests
    {
        [Fact]
        public async Task ValidatedAuthorizedTenantHasTrustedIdAndOperation()
        {
            await using WebApplication app = await CreateAppAsync();
            using HttpClient client = app.GetTestClient();
            using HttpRequestMessage request = CreateRequest("/private", "programaris");
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument json = await ReadAsync(response);
            Assert.Equal("programaris", json.RootElement.GetProperty("slug").GetString());
            Assert.Equal("read", json.RootElement.GetProperty("operation").GetString());
            Assert.Equal(TenantId.ToString(), json.RootElement.GetProperty("tenantId").GetString());
        }

        [Fact]
        public async Task UnauthenticatedCallerCannotAccessValidTenant()
        {
            await using WebApplication app = await CreateAppAsync();
            using HttpClient client = app.GetTestClient();
            using HttpRequestMessage request = CreateRequest("/anonymous-private", "programaris");
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("tenant_authentication_required", await CodeAsync(response));
        }

        [Theory]
        [InlineData("missing", HttpStatusCode.NotFound, "tenant_not_found")]
        [InlineData("inactive", HttpStatusCode.Forbidden, "tenant_inactive")]
        [InlineData("offline", HttpStatusCode.ServiceUnavailable, "tenant_validation_unavailable")]
        [InlineData("inconsistent", HttpStatusCode.ServiceUnavailable, "tenant_validation_unavailable")]
        [InlineData("blocked", HttpStatusCode.Forbidden, "tenant_access_denied")]
        [InlineData("unavailable", HttpStatusCode.ServiceUnavailable, "tenant_access_unavailable")]
        [InlineData("throw-store", HttpStatusCode.ServiceUnavailable, "tenant_validation_unavailable")]
        [InlineData("throw-validator", HttpStatusCode.ServiceUnavailable, "tenant_access_unavailable")]
        public async Task FailClosedForUntrustedOrDeniedTenants(string slug, HttpStatusCode status, string code)
        {
            await using WebApplication app = await CreateAppAsync();
            using HttpClient client = app.GetTestClient();
            using HttpRequestMessage request = CreateRequest("/private", slug);
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(status, response.StatusCode);
            Assert.Equal(code, await CodeAsync(response));
        }

        [Fact]
        public async Task PublicEndpointBypassesValidationAndAuthorization()
        {
            await using WebApplication app = await CreateAppAsync();
            using HttpClient client = app.GetTestClient();
            using HttpResponseMessage response = await client.GetAsync("/public", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task MissingTenantIsRejectedBeforeProtection()
        {
            await using WebApplication app = await CreateAppAsync();
            using HttpClient client = app.GetTestClient();
            using HttpResponseMessage response = await client.GetAsync("/private", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("tenant_header_missing", await CodeAsync(response));
        }

        [Fact]
        public async Task AuthorizedContextNeverLeaksToPublicRequest()
        {
            await using WebApplication app = await CreateAppAsync();
            using HttpClient client = app.GetTestClient();
            using HttpRequestMessage request = CreateRequest("/private", "programaris");
            using HttpResponseMessage first = await client.SendAsync(request, TestContext.Current.CancellationToken);
            using HttpResponseMessage second = await client.GetAsync("/public", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal("false", await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ConcurrentRequestsRemainIsolated()
        {
            await using WebApplication app = await CreateAppAsync();
            using HttpClient client = app.GetTestClient();

            Task[] requests = Enumerable.Range(0, 64).Select(async index =>
            {
                string slug = index % 2 == 0 ? "programaris" : "another";
                using HttpRequestMessage request = CreateRequest("/private", slug);
                using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                using JsonDocument json = await ReadAsync(response);
                Assert.Equal(slug, json.RootElement.GetProperty("slug").GetString());
            }).ToArray();

            await Task.WhenAll(requests);
        }

        [Fact]
        public void AuthorizedAccessorRejectsUnavailableContextAndRepeatedInitializationIsNotPublic()
        {
            var accessor = new TenantAuthorizationContextAccessor();

            Assert.Null(accessor.Current);
            Assert.Throws<TenantAuthorizationContextUnavailableException>(() => accessor.GetRequired());
        }

        private static readonly Guid TenantId = new("1a3256c4-b2c9-4dea-ac64-f3851e79bc91");

        private static HttpRequestMessage CreateRequest(string url, string slug)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Tenant", slug);

            return request;
        }

        private static async Task<JsonDocument> ReadAsync(HttpResponseMessage response)
        {
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        private static async Task<string?> CodeAsync(HttpResponseMessage response)
        {
            using JsonDocument document = await ReadAsync(response);

            return document.RootElement.GetProperty("code").GetString();
        }

        private static async Task<WebApplication> CreateAppAsync()
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddProgramarisTenancy();
            builder.Services.AddProgramarisTenantProtection();
            builder.Services.AddScoped<ITenantStore, FakeStore>();
            builder.Services.AddScoped<ITenantAccessValidator, FakeValidator>();

            WebApplication app = builder.Build();

            app.UseRouting();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path != "/anonymous-private")
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") }, "Test"));
                }

                await next();
            });
            app.UseProgramarisTenancy();
            app.UseProgramarisTenantProtection();

            app.MapGet("/private", (ITenantAuthorizationContextAccessor accessor) =>
            {
                AuthorizedTenantContext authorized = accessor.GetRequired();
                return Results.Ok(new
                {
                    slug = authorized.Tenant.Slug,
                    tenantId = authorized.Tenant.TenantId?.ToString(),
                    operation = authorized.Operation
                });
            }).RequireProgramarisTenantOperation("read");
            app.MapGet("/anonymous-private", () => Results.Ok());
            app.MapGet("/public", (ITenantAuthorizationContextAccessor accessor) =>
                Results.Text((accessor.Current is not null).ToString().ToLowerInvariant())).AllowMissingTenant();

            await app.StartAsync(TestContext.Current.CancellationToken);

            return app;
        }

        private sealed class FakeStore : ITenantStore
        {
            public Task<TenantValidationResult> FindAsync(string tenantSlug, CancellationToken cancellationToken = default)
            {
                if (tenantSlug == "throw-store")
                {
                    throw new InvalidOperationException("Store offline");
                }

                TenantValidationResult result = tenantSlug switch
                {
                    "missing" => TenantValidationResult.NotFound(),
                    "inactive" => TenantValidationResult.Inactive(),
                    "offline" => TenantValidationResult.Unavailable(),
                    "inconsistent" => TenantValidationResult.Active(new TenantContext("wrong")),
                    _ => TenantValidationResult.Active(new TenantContext(tenantSlug, TenantId))
                };

                return new Task<TenantValidationResult>(result);
            }
        }

        private sealed class FakeValidator : ITenantAccessValidator
        {
            public Task<TenantAccessResult> ValidateAsync(TenantAccessRequest request, CancellationToken cancellationToken = default)
            {
                if (request.TenantSlug == "throw-validator")
                {
                    throw new InvalidOperationException("Identity offline");
                }

                TenantAccessResult result = request.TenantSlug switch
                {
                    "blocked" => TenantAccessResult.Denied(),
                    "unavailable" => TenantAccessResult.Unavailable(),
                    _ => TenantAccessResult.Allowed()
                };

                return new Task<TenantAccessResult>(result);
            }
        }
    }
}
