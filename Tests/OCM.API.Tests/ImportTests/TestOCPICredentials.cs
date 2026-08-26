using OCM.Import.Providers.OCPI;
using Xunit;

namespace OCM.API.Tests.ImportTests
{
    /// <summary>
    /// Covers the credential key naming used when an approved import has its secret auto-created in the
    /// key vault, and the auth header composition that credential verification has to reproduce exactly.
    /// </summary>
    public class TestOCPICredentials
    {
        [Theory]
        [InlineData("evio", "OCPI-EVIO")]
        [InlineData("go-evio", "OCPI-GO-EVIO")]
        [InlineData("lakd.lt", "OCPI-LAKD-LT")]
        [InlineData("My Charging Network", "OCPI-MY-CHARGING-NETWORK")]
        [InlineData("  spaced  out  ", "OCPI-SPACED-OUT")]
        [InlineData("weird!!!chars???", "OCPI-WEIRD-CHARS")]
        public void BuildCredentialKey_DerivesVaultSafeName(string providerName, string expected)
        {
            Assert.Equal(expected, OCPICredentialNaming.BuildCredentialKey(providerName));
            Assert.True(OCPICredentialNaming.IsValidSecretName(OCPICredentialNaming.BuildCredentialKey(providerName)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("!!!")]
        public void BuildCredentialKey_ReturnsNullWhenNoUsableName(string providerName)
        {
            Assert.Null(OCPICredentialNaming.BuildCredentialKey(providerName));
        }

        [Fact]
        public void BuildCredentialKey_TruncatesToVaultLimit()
        {
            var key = OCPICredentialNaming.BuildCredentialKey(new string('a', 200));

            Assert.NotNull(key);
            Assert.True(key.Length <= OCPICredentialNaming.MaxSecretNameLength);
            Assert.True(OCPICredentialNaming.IsValidSecretName(key));
        }

        [Theory]
        [InlineData("OCPI-EVIO", "OCPI-EVIO")]
        [InlineData("ocpi-evio", "OCPI-EVIO")]
        [InlineData("EVIO", "OCPI-EVIO")]
        [InlineData("my provider", "OCPI-MY-PROVIDER")]
        public void NormaliseCredentialKey_KeepsSinglePrefix(string supplied, string expected)
        {
            Assert.Equal(expected, OCPICredentialNaming.NormaliseCredentialKey(supplied));
        }

        [Theory]
        [InlineData("OCPI EVIO")]
        [InlineData("OCPI-EVIO ")]
        [InlineData("")]
        [InlineData(null)]
        public void IsValidSecretName_RejectsUnusableNames(string secretName)
        {
            Assert.False(OCPICredentialNaming.IsValidSecretName(secretName));
        }

        [Fact]
        public void ComposeAuthHeaderValue_AppliesPrefixToUnprefixedCredential()
        {
            Assert.Equal("Token abc123", ImportProvider_OCPI.ComposeAuthHeaderValue("Authorization", "Token ", "abc123"));
        }

        [Fact]
        public void ComposeAuthHeaderValue_LeavesRecognisedPrefixesAlone()
        {
            Assert.Equal("Token abc123", ImportProvider_OCPI.ComposeAuthHeaderValue("Authorization", "Token ", "Token abc123"));
            Assert.Equal("Bearer abc123", ImportProvider_OCPI.ComposeAuthHeaderValue("Authorization", "Token ", "Bearer abc123"));
            Assert.Equal("Basic abc123", ImportProvider_OCPI.ComposeAuthHeaderValue("Authorization", "Token ", "Basic abc123"));
        }

        [Fact]
        public void ComposeAuthHeaderValue_DoesNotPrefixCustomHeaders()
        {
            // e.g. providers using an "apikey" header expect the raw credential
            Assert.Equal("abc123", ImportProvider_OCPI.ComposeAuthHeaderValue("apikey", "Token ", "abc123"));
        }

        [Fact]
        public void ComposeAuthHeaderValue_TreatsNullPrefixAsNoPrefix()
        {
            Assert.Equal("abc123", ImportProvider_OCPI.ComposeAuthHeaderValue("Authorization", null, "abc123"));
        }

        [Fact]
        public void ComposeAuthHeaderValue_MatchesProviderBehaviour()
        {
            // the verification path must send exactly what a configured import provider would send
            var provider = new ImportProvider_OCPIConfigurable(new OCPIProviderConfiguration
            {
                ProviderName = "test-provider",
                DataProviderId = 1,
                LocationsEndpointUrl = "https://example.com/ocpi/2.2/locations",
                AuthHeaderKey = "Authorization",
                AuthHeaderValuePrefix = "Token ",
                CredentialKey = "OCPI-TEST-PROVIDER"
            });

            provider.AuthHeaderValue = "abc123";

            Assert.Equal(
                provider.AuthHeaderValue,
                ImportProvider_OCPI.ComposeAuthHeaderValue("Authorization", "Token ", "abc123"));
        }
    }
}
