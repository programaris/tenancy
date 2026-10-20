using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Programaris.Tenancy.Abstractions;

using Xunit;

namespace Programaris.Tenancy.AspNetCore.Tests
{
    public sealed class TenantSelectionSourceTests
    {
        [Fact]
        public async Task ExplicitlyEnabledQuerySourceCanSelectTenantForBrowserRequest()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/oidc?tenant-slug=programaris", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("programaris", await ReadSlugAsync(response));
        }

        [Fact]
        public async Task QuerySourceIsNotEnabledOnOtherEndpoints()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/plain?tenant-slug=programaris", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("tenant_header_missing", await ReadCodeAsync(response));
        }

        [Fact]
        public async Task MatchingHeaderAndBrowserSelectionAreAccepted()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/oidc?tenant-slug=programaris");
            request.Headers.Add("X-Tenant", "programaris");
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("programaris", await ReadSlugAsync(response));
        }

        [Theory]
        [InlineData("/oidc?tenant-slug=other", "tenant_selection_conflict")]
        [InlineData("/oidc?tenant-slug=INVALID", "tenant_selection_invalid")]
        [InlineData("/oidc?tenant-slug=programaris&tenant-slug=programaris", "tenant_selection_invalid")]
        public async Task BrowserCandidateIsRejectedWhenConflictingInvalidOrRepeated(string path, string code)
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (code == "tenant_selection_conflict")
            {
                request.Headers.Add("X-Tenant", "programaris");
            }

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(code, await ReadCodeAsync(response));
        }

        [Fact]
        public async Task TwoEnabledSourcesCannotDisagree()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/multiple?tenant-slug=programaris&other-tenant=another", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("tenant_selection_conflict", await ReadCodeAsync(response));
        }

        [Fact]
        public async Task FormSourceSelectsOnlyWhenExplicitlyEnabled()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var response = await client.PostAsync(
                "/form",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["tenant-slug"] = "programaris" }),
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("programaris", await ReadSlugAsync(response));
        }

        [Fact]
        public async Task DuplicateFormSelectionIsRejected()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var values = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("tenant-slug", "programaris"),
                new KeyValuePair<string, string>("tenant-slug", "programaris")
            });
            using var response = await client.PostAsync("/form", values, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("tenant_selection_invalid", await ReadCodeAsync(response));
        }

        [Fact]
        public async Task MissingRegisteredProviderFailsClosed()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/missing-provider", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("tenant_selection_source_unavailable", await ReadCodeAsync(response));
        }

        [Fact]
        public async Task ThrowingProviderFailsClosed()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/throwing-provider", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("tenant_selection_source_unavailable", await ReadCodeAsync(response));
        }

        [Fact]
        public async Task MissingSelectionIsNotImplicitlyDefaulted()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/oidc", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("tenant_header_missing", await ReadCodeAsync(response));
        }

        [Fact]
        public async Task ConcurrentBrowserSelectionsRemainRequestLocal()
        {
            await using var app = await CreateAppAsync();
            using var client = app.GetTestClient();

            Task[] tasks = Enumerable.Range(0, 48).Select(async index =>
            {
                string slug = index % 2 == 0 ? "programaris" : "another";
                using var response = await client.GetAsync(
                    $"/oidc?tenant-slug={slug}", TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(slug, await ReadSlugAsync(response));
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        private static async Task<string> ReadSlugAsync(HttpResponseMessage response)
        {
            using JsonDocument body = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return body.RootElement.GetProperty("slug").GetString()!;
        }

        private static async Task<string> ReadCodeAsync(HttpResponseMessage response)
        {
            using JsonDocument body = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return body.RootElement.GetProperty("code").GetString()!;
        }

        private static async Task<WebApplication> CreateAppAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddProgramarisTenancy();
            builder.Services.AddScoped<QuerySelectionSource>();
            builder.Services.AddScoped<OtherQuerySelectionSource>();
            builder.Services.AddScoped<FormSelectionSource>();
            builder.Services.AddScoped<ThrowingSelectionSource>();

            WebApplication app = builder.Build();
            app.UseRouting();
            app.UseProgramarisTenancy();

            app.MapGet("/plain", (ITenantContextAccessor tenant) =>
                Results.Ok(new { slug = tenant.GetRequired().Slug }));

            app.MapGet("/oidc", (ITenantContextAccessor tenant) =>
                Results.Ok(new { slug = tenant.GetRequired().Slug }))
                .UseProgramarisTenantSelectionSource<QuerySelectionSource>();

            app.MapGet("/multiple", (ITenantContextAccessor tenant) =>
                Results.Ok(new { slug = tenant.GetRequired().Slug }))
                .UseProgramarisTenantSelectionSource<QuerySelectionSource>()
                .UseProgramarisTenantSelectionSource<OtherQuerySelectionSource>();

            app.MapPost("/form", (ITenantContextAccessor tenant) =>
                Results.Ok(new { slug = tenant.GetRequired().Slug }))
                .UseProgramarisTenantSelectionSource<FormSelectionSource>();

            app.MapGet("/missing-provider", () => Results.Ok())
                .UseProgramarisTenantSelectionSource<UnregisteredSelectionSource>();

            app.MapGet("/throwing-provider", () => Results.Ok())
                .UseProgramarisTenantSelectionSource<ThrowingSelectionSource>();

            await app.StartAsync(TestContext.Current.CancellationToken);
            return app;
        }

        private sealed class QuerySelectionSource : ITenantSelectionSource
        {
            public Task<IReadOnlyList<string>> GetCandidatesAsync(HttpContext context, CancellationToken cancellationToken = default)
            {
                IReadOnlyList<string> values = context.Request.Query.TryGetValue("tenant-slug", out var candidates)
                    ? candidates.Select(candidate => candidate ?? string.Empty).ToArray()
                    : Array.Empty<string>();
                return Task.FromResult(values);
            }
        }

        private sealed class OtherQuerySelectionSource : ITenantSelectionSource
        {
            public Task<IReadOnlyList<string>> GetCandidatesAsync(HttpContext context, CancellationToken cancellationToken = default)
            {
                IReadOnlyList<string> values = context.Request.Query.TryGetValue("other-tenant", out var candidates)
                    ? candidates.Select(candidate => candidate ?? string.Empty).ToArray()
                    : Array.Empty<string>();
                return Task.FromResult(values);
            }
        }

        private sealed class FormSelectionSource : ITenantSelectionSource
        {
            public async Task<IReadOnlyList<string>> GetCandidatesAsync(HttpContext context, CancellationToken cancellationToken = default)
            {
                if (!context.Request.HasFormContentType)
                {
                    return Array.Empty<string>();
                }

                var form = await context.Request.ReadFormAsync(cancellationToken);
                return form.TryGetValue("tenant-slug", out var candidates)
                    ? candidates.Select(candidate => candidate ?? string.Empty).ToArray()
                    : Array.Empty<string>();
            }
        }

        private sealed class ThrowingSelectionSource : ITenantSelectionSource
        {
            public Task<IReadOnlyList<string>> GetCandidatesAsync(HttpContext context, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Test source failure.");
            }
        }

        private sealed class UnregisteredSelectionSource : ITenantSelectionSource
        {
            public Task<IReadOnlyList<string>> GetCandidatesAsync(HttpContext context, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            }
        }
    }
}
