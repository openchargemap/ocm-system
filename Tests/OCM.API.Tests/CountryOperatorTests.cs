using OCM.API.Common;
using Xunit;

namespace OCM.API.Tests
{
    public class CountryOperatorTests
    {
        [Theory]
        [InlineData(" Example Charge (ES) ", "examplechargees")]
        [InlineData("Example-Charge", "examplecharge")]
        public void Duplicate_title_normalization_is_case_and_punctuation_insensitive(string title, string expected)
        {
            Assert.Equal(expected, OperatorInfoManager.NormalizeTitle(title));
        }

        [Theory]
        [InlineData("https://www.example.com/path", "example.com")]
        [InlineData("example.com", "example.com")]
        public void Duplicate_website_matching_uses_the_normalized_host(string url, string expected)
        {
            Assert.Equal(expected, OperatorInfoManager.GetWebsiteHost(url));
        }

    }
}
