using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Programaris.Tenancy.Abstractions;

using Xunit;

namespace Programaris.Tenancy.AspNetCore.Tests
{
    public sealed class HttpTenancyTests
    {
        [Fact]
        public async Task ValidHeaderIsAvailableThroughScopedAccessor()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/tenant");

            request.Headers.Add("X-Tenant", "another");

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("another", await ReadPropertyAsync(response, "slug"));
        }

        [Fact]
        public async Task MissingHeaderFailsClosed()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/v1/tenant", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("tenant_header_missing", await ReadPropertyAsync(response, "code"));
        }

        [Fact]
        public async Task PublicEndpointAllowsMissingHeader()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task OptionalEndpointHasNoContextWhenHeaderIsMissing()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/v1/optional", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(await ReadBooleanPropertyAsync(response, "hasTenant"));
        }

        [Theory]
        [InlineData("BAD")]
        [InlineData("invalid tenant")]
        [InlineData("one,two")]
        [InlineData("a_b")]
        public async Task InvalidHeaderIsRejectedEvenOnOptionalRoute(string tenant)
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/health");

            request.Headers.TryAddWithoutValidation("X-Tenant", tenant);

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("tenant_header_invalid", await ReadPropertyAsync(response, "code"));
        }

        [Fact]
        public async Task EmptyHeaderOnRequiredRouteIsRejected()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/tenant");

            request.Headers.TryAddWithoutValidation("X-Tenant", string.Empty);

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            // Different HTTP transports may preserve an empty header or omit it.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DuplicateHeadersAreRejected()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/tenant");

            request.Headers.TryAddWithoutValidation("X-Tenant", new[] { "programaris", "another" });

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("tenant_header_invalid", await ReadPropertyAsync(response, "code"));
        }

        [Fact]
        public async Task CustomHeaderNameIsSupported()
        {
            await using var app = await CreateAppAsync(options => options.HeaderName = "X-Tenant-Slug");
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/tenant");

            request.Headers.Add("X-Tenant-Slug", "another");

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("another", await ReadPropertyAsync(response, "slug"));
        }

        [Fact]
        public async Task AllowMissingTenantDoesNotMakeOtherEndpointsOptional()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();

            using var optional = await client.GetAsync("/v1/optional", TestContext.Current.CancellationToken);
            using var required = await client.GetAsync("/v1/tenant", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, optional.StatusCode);
            Assert.False(await ReadBooleanPropertyAsync(optional, "hasTenant"));
            Assert.Equal(HttpStatusCode.BadRequest, required.StatusCode);
            Assert.Equal("tenant_header_missing", await ReadPropertyAsync(required, "code"));
        }

        [Fact]
        public async Task OptionalEndpointNeverInheritsTenantFromPreviousRequest()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var selectedRequest = new HttpRequestMessage(HttpMethod.Get, "/v1/tenant");

            selectedRequest.Headers.Add("X-Tenant", "programaris");

            using var selectedResponse = await client.SendAsync(selectedRequest, TestContext.Current.CancellationToken);
            using var optionalResponse = await client.GetAsync("/v1/optional", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, selectedResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, optionalResponse.StatusCode);
            Assert.False(await ReadBooleanPropertyAsync(optionalResponse, "hasTenant"));
        }

        [Fact]
        public async Task MissingTenantDoesNotLeakBetweenSequentialRequests()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var first = new HttpRequestMessage(HttpMethod.Get, "/v1/tenant");

            first.Headers.Add("X-Tenant", "programaris");

            using var firstResult = await client.SendAsync(first, TestContext.Current.CancellationToken);
            using var secondResult = await client.GetAsync("/v1/tenant", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, firstResult.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, secondResult.StatusCode);
        }

        [Fact]
        public async Task GetRequiredThrowsWhenNoTenantContextIsInitialized()
        {
            await using var app = await CreateAppAsync();
            using var scope = app.Services.CreateScope();
            var accessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();

            Assert.Null(accessor.Current);
            Assert.Throws<TenantContextUnavailableException>(() => accessor.GetRequired());
        }

        [Fact]
        public async Task ConcurrentRequestsPreserveTheirOwnTenant()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();

            Task[] requests = Enumerable.Range(0, 64).Select(async index =>
            {
                string expected = index % 2 == 0 ? "programaris" : "another";
                using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/tenant-delayed");

                request.Headers.Add("X-Tenant", expected);

                using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(expected, await ReadPropertyAsync(response, "slug"));
                Assert.True(await ReadBooleanPropertyAsync(response, "unchanged"));
            }).ToArray();

            await Task.WhenAll(requests);
        }

        [Fact]
        public async Task TenantHeaderOnOptionalEndpointStillSetsContext()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/optional");

            request.Headers.Add("X-Tenant", "programaris");

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(await ReadBooleanPropertyAsync(response, "hasTenant"));
        }

        [Fact]
        public void OutboundPropagationRequiresAnEstablishedTenant()
        {
            var accessor = new TenantContextAccessor();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.invalid/");

            Assert.Throws<TenantContextUnavailableException>(() => request.AddProgramarisTenant(accessor));
            Assert.False(request.Headers.Contains("X-Tenant"));
        }

        [Fact]
        public void ExplicitOutboundPropagationDoesNotReplaceExistingHeader()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.invalid/");

            request.AddProgramarisTenant(new TenantContext("another"));

            Assert.Equal("another", request.Headers.GetValues("X-Tenant").Single());
            Assert.Throws<InvalidOperationException>(() => request.AddProgramarisTenant(new TenantContext("programaris")));
        }

        [Fact]
        public void ExplicitOutboundPropagationSupportsCustomHeader()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.invalid/");

            request.AddProgramarisTenant(new TenantContext("another"), "X-Tenant-Slug");

            Assert.Equal("another", request.Headers.GetValues("X-Tenant-Slug").Single());
        }

        [Fact]
        public void ExplicitOutboundPropagationRejectsInvalidSlug()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.invalid/");

            Assert.Throws<ArgumentException>(() => request.AddProgramarisTenant(new TenantContext("INVALID")));
        }

        [Theory]
        [InlineData("")]
        [InlineData("X Tenant")]
        [InlineData("X-Tenant\r\nInjected")]
        public void ExplicitOutboundPropagationRejectsInvalidHeaderName(string headerName)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.invalid/");

            Assert.Throws<ArgumentException>(() => request.AddProgramarisTenant(new TenantContext("another"), headerName));
        }

        [Fact]
        public async Task InvalidStartupOptionsAreRejected()
        {
            var builder = WebApplication.CreateBuilder();

            builder.WebHost.UseTestServer();
            builder.Services.AddProgramarisTenancy(options => options.HeaderName = "X Tenant");

            await using var app = builder.Build();

            app.UseRouting();
            app.UseProgramarisTenancy();
            app.MapGet("/health", () => Results.Ok()).AllowMissingTenant();

            await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync(TestContext.Current.CancellationToken));
        }

        private static async Task<string?> ReadPropertyAsync(HttpResponseMessage response, string name)
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

            return body.RootElement.GetProperty(name).GetString();
        }

        private static async Task<bool> ReadBooleanPropertyAsync(HttpResponseMessage response, string name)
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

            return body.RootElement.GetProperty(name).GetBoolean();
        }

        private static async Task<WebApplication> CreateAppAsync(Action<TenantHeaderOptions>? configure = null)
        {
            var builder = WebApplication.CreateBuilder();

            builder.WebHost.UseTestServer();
            builder.Services.AddProgramarisTenancy(configure);

            WebApplication app = builder.Build();

            app.UseRouting();
            app.UseProgramarisTenancy();
            app.MapGet("/health", () => Results.Ok()).AllowMissingTenant();
            app.MapGet("/v1/tenant", (ITenantContextAccessor accessor) => Results.Ok(new { slug = accessor.GetRequired().Slug }));
            app.MapGet("/v1/optional", (ITenantContextAccessor accessor) => Results.Ok(new { hasTenant = accessor.Current is not null })).AllowMissingTenant();
            app.MapGet("/v1/tenant-delayed", async (ITenantContextAccessor accessor, CancellationToken cancellationToken) =>
            {
                string before = accessor.GetRequired().Slug;
                await Task.Delay(5, cancellationToken);
                string after = accessor.GetRequired().Slug;

                return Results.Ok(new { slug = after, unchanged = before == after });
            });

            await app.StartAsync();

            return app;
        }
    }
}