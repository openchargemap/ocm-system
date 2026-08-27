using OCM.API.Common;
using Xunit;

namespace OCM.API.Tests
{
    public class OperatorInfoManagerTests
    {
        [Theory]
        [InlineData(" Example Charge (ES) ", "examplechargees")]
        [InlineData("Example-Charge", "examplecharge")]
        public void NormalizeTitle_is_case_and_punctuation_insensitive(string title, string expected)
        {
            Assert.Equal(expected, OperatorInfoManager.NormalizeTitle(title));
        }

        [Theory]
        [InlineData("https://www.example.com/path", "example.com")]
        [InlineData("example.com", "example.com")]
        public void GetWebsiteHost_returns_normalized_host(string url, string expected)
        {
            Assert.Equal(expected, OperatorInfoManager.GetWebsiteHost(url));
        }
    }
}
