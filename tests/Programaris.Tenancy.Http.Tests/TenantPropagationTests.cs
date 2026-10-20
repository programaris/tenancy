using System.Net;

using Microsoft.Extensions.Options;

using Programaris.Tenancy.Abstractions;

using Xunit;

namespace Programaris.Tenancy.Http.Tests
{
    public sealed class TenantPropagationTests
    {
        [Fact]
        public async Task ApprovedDestinationAndExplicitTenantAreAccepted()
        {
            using var client = CreateClient(TenantPropagationMode.Required);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://identity.programaris.com.br/v1/users");
            request.WithProgramarisTenant(new TenantContext("programaris"));
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("programaris", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task RequiredModeRejectsMissingTenant()
        {
            using var client = CreateClient(TenantPropagationMode.Required);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://identity.programaris.com.br/v1/users");

            await Assert.ThrowsAsync<TenantPropagationException>(
                () => client.SendAsync(request, TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData("https://attacker.example/v1/users")]
        [InlineData("https://identity.programaris.com.br.attacker.example/")]
        [InlineData("http://identity.programaris.com.br/v1/users")]
        [InlineData("https://identity.programaris.com.br:444/v1/users")]
        public async Task RefusesUnapprovedOrInsecureDestinations(string destination)
        {
            using var client = CreateClient(TenantPropagationMode.Optional);
            using var request = new HttpRequestMessage(HttpMethod.Get, destination);
            request.WithProgramarisTenant(new TenantContext("programaris"));

            await Assert.ThrowsAsync<TenantPropagationException>(
                () => client.SendAsync(request, TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData("https://user@identity.programaris.com.br/v1/users")]
        [InlineData("https://user:password@identity.programaris.com.br/v1/users")]
        public async Task CredentialedDestinationsAreRejected(string destination)
        {
            using var client = CreateClient(TenantPropagationMode.Required);
            using var request = new HttpRequestMessage(HttpMethod.Get, destination);
            request.WithProgramarisTenant(new TenantContext("programaris"));

            await Assert.ThrowsAsync<TenantPropagationException>(
                () => client.SendAsync(request, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void ConfigurationRequiresExactHttpsOrigins()
        {
            var missing = new TenantHttpClientOptions();
            Assert.Throws<ArgumentException>(() => new TenantPropagationHandler(Options.Create(missing)));

            var insecure = new TenantHttpClientOptions();
            insecure.AllowedOrigins.Add(new Uri("http://identity.programaris.com.br"));
            Assert.Throws<ArgumentException>(() => new TenantPropagationHandler(Options.Create(insecure)));

            var path = new TenantHttpClientOptions();
            path.AllowedOrigins.Add(new Uri("https://identity.programaris.com.br/v1/"));
            Assert.Throws<ArgumentException>(() => new TenantPropagationHandler(Options.Create(path)));
        }

        [Fact]
        public async Task PreexistingValidTenantHeaderIsRejected()
        {
            using var client = CreateClient(TenantPropagationMode.Optional);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://identity.programaris.com.br/");
            request.Headers.Add("X-Tenant", "programaris");

            await Assert.ThrowsAsync<TenantPropagationException>(
                () => client.SendAsync(request, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void TenantHeaderIsInjectedOnlyAfterDestinationValidation()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://identity.programaris.com.br/");
            request.WithProgramarisTenant(new TenantContext("programaris"));

            Assert.False(request.Headers.Contains("X-Tenant"));
        }

        [Fact]
        public async Task InvalidTenantHeaderFailsBeforeSending()
        {
            using var client = CreateClient(TenantPropagationMode.Optional);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://identity.programaris.com.br/");
            request.Headers.TryAddWithoutValidation("X-Tenant", "INVALID");

            await Assert.ThrowsAsync<TenantPropagationException>(
                () => client.SendAsync(request, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void ExplicitTenantCannotOverwriteAnExistingHeader()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://identity.programaris.com.br/");
            request.WithProgramarisTenant(new TenantContext("programaris"));

            Assert.Throws<TenantPropagationException>(() =>
                request.WithProgramarisTenant(new TenantContext("another")));
        }

        [Fact]
        public async Task ConcurrentRequestsKeepIndependentTenantHeaders()
        {
            using var client = CreateClient(TenantPropagationMode.Required);

            Task[] tasks = Enumerable.Range(0, 64).Select(async index =>
            {
                string slug = index % 2 == 0 ? "programaris" : "another";
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://identity.programaris.com.br/");
                request.WithProgramarisTenant(new TenantContext(slug));
                using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

                Assert.Equal(slug, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        private static HttpClient CreateClient(TenantPropagationMode mode)
        {
            var options = new TenantHttpClientOptions { Mode = mode };
            options.AllowedOrigins.Add(new Uri("https://identity.programaris.com.br"));

            var handler = new TenantPropagationHandler(Options.Create(options))
            {
                InnerHandler = new EchoTenantHandler()
            };

            return new HttpClient(handler);
        }

        private sealed class EchoTenantHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                string slug = request.Headers.TryGetValues("X-Tenant", out IEnumerable<string>? values)
                    ? values.Single()
                    : "none";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(slug)
                });
            }
        }
    }
}
