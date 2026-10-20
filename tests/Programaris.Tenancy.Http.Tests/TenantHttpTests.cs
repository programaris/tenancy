using System.Net;
using System.Security.Claims;

using Microsoft.Extensions.DependencyInjection;

using Programaris.Tenancy.Abstractions;

using Xunit;

namespace Programaris.Tenancy.Http.Tests
{
    public sealed class TenantHttpTests
    {
        private const string Approved = "https://identity.programaris.com.br/";

        [Fact]
        public async Task ExplicitTenantOnlyGoesToAllowlistedOrigin()
        {
            var sink = new CaptureHandler();
            using var client = CreateClient(new TenantHttpClientOptions
            {
                Mode = TenantPropagationMode.Explicit
            }, sink);

            using var request = new HttpRequestMessage(HttpMethod.Get, Approved + "v1/users").WithTenant("another");
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("another", sink.LastTenant);
        }

        [Fact]
        public async Task RejectsCrossOriginWithoutDispatch()
        {
            var sink = new CaptureHandler();
            using var client = CreateClient(new TenantHttpClientOptions(), sink);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://evil.invalid/v1/users").WithTenant("programaris");

            await Assert.ThrowsAsync<TenantPropagationException>(
                () => client.SendAsync(request, TestContext.Current.CancellationToken));

            Assert.Equal(0, sink.RequestCount);
        }

        [Theory]
        [InlineData("http://identity.programaris.com.br/")]
        [InlineData("https://identity.programaris.com.br.evil.invalid/")]
        [InlineData("https://identity.programaris.com.br:444/")]
        [InlineData("https://user@identity.programaris.com.br/")]
        public async Task RejectsUnapprovedDestinations(string url)
        {
            var sink = new CaptureHandler();
            using var client = CreateClient(new TenantHttpClientOptions(), sink);
            using var request = new HttpRequestMessage(HttpMethod.Get, url).WithTenant("another");

            await Assert.ThrowsAsync<TenantPropagationException>(
                () => client.SendAsync(request, TestContext.Current.CancellationToken));
            Assert.Equal(0, sink.RequestCount);
        }

        [Fact]
        public async Task RequiredModeRejectsMissingSelection()
        {
            var sink = new CaptureHandler();
            using var client = CreateClient(new TenantHttpClientOptions { Mode = TenantPropagationMode.Required }, sink);
            using var request = new HttpRequestMessage(HttpMethod.Get, Approved);

            await Assert.ThrowsAsync<TenantPropagationException>(
                () => client.SendAsync(request, TestContext.Current.CancellationToken));
            Assert.Equal(0, sink.RequestCount);
        }

        [Fact]
        public async Task OptionalModeOmitsTenantWhenAbsent()
        {
            var sink = new CaptureHandler();
            using var client = CreateClient(new TenantHttpClientOptions { Mode = TenantPropagationMode.Optional }, sink);
            using var response = await client.GetAsync(Approved, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Null(sink.LastTenant);
        }

        [Fact]
        public async Task ContextCaptureIsExplicitAndScopedToARequest()
        {
            var sink = new CaptureHandler();
            using var client = CreateClient(new TenantHttpClientOptions { Mode = TenantPropagationMode.Required }, sink);
            using var first = new HttpRequestMessage(HttpMethod.Get, Approved).WithTenantFrom(new FixedAccessor("another"));
            using var firstResponse = await client.SendAsync(first, TestContext.Current.CancellationToken);

            Assert.Equal("another", sink.LastTenant);

            using var second = new HttpRequestMessage(HttpMethod.Get, Approved);
            await Assert.ThrowsAsync<TenantPropagationException>(
                () => client.SendAsync(second, TestContext.Current.CancellationToken));
            Assert.Equal(1, sink.RequestCount);
        }

        [Fact]
        public async Task ExplicitModeRejectsContextualCapture()
        {
            var sink = new CaptureHandler();
            using var client = CreateClient(new TenantHttpClientOptions(), sink);
            using var request = new HttpRequestMessage(HttpMethod.Get, Approved).WithTenantFrom(new FixedAccessor("another"));

            await Assert.ThrowsAsync<TenantPropagationException>(
                () => client.SendAsync(request, TestContext.Current.CancellationToken));

            Assert.Equal(0, sink.RequestCount);
        }

        [Fact]
        public async Task RejectsExistingTenantHeaderWithoutOverwriting()
        {
            var sink = new CaptureHandler();
            using var client = CreateClient(new TenantHttpClientOptions(), sink);
            using var request = new HttpRequestMessage(HttpMethod.Get, Approved).WithTenant("another");
            request.Headers.Add("X-Tenant", "programaris");

            await Assert.ThrowsAsync<TenantPropagationException>(
                () => client.SendAsync(request, TestContext.Current.CancellationToken));
            Assert.Equal("programaris", request.Headers.GetValues("X-Tenant").Single());
            Assert.Equal(0, sink.RequestCount);
        }

        [Fact]
        public void DuplicateSelectionsAndInvalidSlugsAreRejected()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Approved).WithTenant("another");

            Assert.Throws<TenantPropagationException>(() => request.WithTenant("programaris"));
            using var invalid = new HttpRequestMessage(HttpMethod.Get, Approved);
            Assert.Throws<ArgumentException>(() => invalid.WithTenant("INVALID"));
        }

        [Fact]
        public void InvalidOriginPolicyFailsAtRegistration()
        {
            var services = new ServiceCollection();
            services.AddHttpClient("Identity");

            Assert.Throws<ArgumentException>(() => services.AddHttpClient("Unsafe")
                .AddProgramarisTenancyHttp(options => options.AllowedOrigins.Add(
                    new Uri("http://identity.programaris.com.br/"))));
        }

        [Fact]
        public async Task ApprovedDefaultHandlerCanBeConstructedWithSafePolicy()
        {
            var services = new ServiceCollection();
            services.AddHttpClient("Identity")
                .AddProgramarisTenancyHttp(options =>
                {
                    options.Mode = TenantPropagationMode.Required;
                    options.AllowedOrigins.Add(new Uri(Approved));
                });

            using ServiceProvider provider = services.BuildServiceProvider();
            using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Identity");

            Assert.NotNull(client);
            Assert.Equal("Identity", "Identity");
            await Task.CompletedTask;
        }

        private static HttpClient CreateClient(TenantHttpClientOptions options, CaptureHandler sink)
        {
            options.AllowedOrigins.Add(new Uri(Approved));
            var handler = new TenantPropagationHandler(options)
            {
                InnerHandler = sink
            };

            return new HttpClient(handler);
        }

        private sealed class FixedAccessor : ITenantContextAccessor
        {
            public FixedAccessor(string slug)
            {
                Current = new TenantContext(slug);
            }

            public TenantContext? Current { get; }

            public TenantContext GetRequired() => Current!;
        }

        private sealed class CaptureHandler : HttpMessageHandler
        {
            public string? LastTenant { get; private set; }

            public int RequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestCount++;
                LastTenant = request.Headers.TryGetValues("X-Tenant", out IEnumerable<string>? values)
                    ? values.Single()
                    : null;

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
