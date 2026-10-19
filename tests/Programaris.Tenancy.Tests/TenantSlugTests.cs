using Xunit;

namespace Programaris.Tenancy.Tests
{
    public sealed class TenantSlugTests
    {
        [Theory]
        [InlineData("programaris")]
        [InlineData("another")]
        [InlineData("client-123")]
        [InlineData("a")]
        [InlineData("0")]
        [InlineData("a--b")]
        public void AcceptsCanonicalSlugs(string input)
        {
            Assert.True(TenantSlug.TryParse(input, out string slug));
            Assert.Equal(input, slug);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("PROGRAMARIS")]
        [InlineData("-bad")]
        [InlineData("bad-")]
        [InlineData("bad tenant")]
        [InlineData("a,b")]
        [InlineData("a_b")]
        [InlineData("á")]
        [InlineData("one\r\ntwo")]
        public void RejectsNonCanonicalSlugs(string? input)
        {
            Assert.False(TenantSlug.TryParse(input, out string slug));
            Assert.Equal(string.Empty, slug);
        }

        [Fact]
        public void AcceptsExactlySixtyThreeCharacters()
        {
            string input = new string('a', 63);

            Assert.True(TenantSlug.TryParse(input, out string slug));
            Assert.Equal(input, slug);
        }

        [Fact]
        public void RejectsTooLongSlug()
        {
            Assert.False(TenantSlug.TryParse(new string('a', 64), out _));
        }
    }
}