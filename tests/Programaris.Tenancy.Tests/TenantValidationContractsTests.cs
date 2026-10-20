using System.Security.Claims;

using Programaris.Tenancy.Abstractions;

using Xunit;

namespace Programaris.Tenancy.Tests
{
    public sealed class TenantValidationContractsTests
    {
        [Fact]
        public void TenantFoundDoesNotConferAccess()
        {
            TenantValidationResult result = TenantValidationResult.Found(new TenantContext("programaris"));
            Assert.True(result.IsValid);
            Assert.False(TenantAccessResult.Denied().IsGranted);
            Assert.False(TenantAccessResult.Unavailable().IsGranted);
        }

        [Fact]
        public void TenantUnavailableIsNotFoundOrAuthorized()
        {
            TenantValidationResult result = TenantValidationResult.Unavailable();
            Assert.Equal(TenantValidationStatus.Unavailable, result.Status);
            Assert.False(result.IsValid);
            Assert.Null(result.Tenant);
        }

        [Fact]
        public void AccessRequestRejectsMissingIdentity()
        {
            Assert.Throws<ArgumentNullException>(() => new TenantAccessRequest("programaris", null!));
            var request = new TenantAccessRequest("programaris", new ClaimsPrincipal());
            Assert.Equal("programaris", request.TenantSlug);
        }
    }
}
