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

        [Theory]
        [InlineData("Ionity (DE)", "DE")]
        [InlineData("EVgo (us)", "US")]
        [InlineData("Tesla (Supercharger)", null)]
        [InlineData("Circuit électrique", null)]
        [InlineData("Ionity", null)]
        public void Country_code_is_read_from_the_operator_title_suffix(string title, string expected)
        {
            Assert.Equal(expected, OperatorInfoManager.GetCountryCodeFromTitle(title));
        }

        [Theory]
        [InlineData("Ionity (DE)", "Ionity")]
        [InlineData("Tesla (Supercharger)", "Tesla (Supercharger)")]
        public void Country_code_suffix_is_removed_before_names_are_compared(string title, string expected)
        {
            Assert.Equal(expected, OperatorInfoManager.RemoveCountryCode(title));
        }

        [Theory]
        // generic charging terms and legal suffixes carry no meaning, so are dropped
        [InlineData("EV Connect", "connect")]
        [InlineData("Allego Charging B.V.", "allego")]
        // a name made up entirely of generic terms keeps them, otherwise every such name would look alike
        [InlineData("EV Charge", "chargeev")]
        public void Name_comparison_ignores_generic_charging_and_legal_words(string title, string expected)
        {
            Assert.Equal(expected, OperatorInfoManager.GetComparisonName(title));
        }

        [Theory]
        [InlineData("Ionity (DE)", "Ionity")]                     // same operator, country suffix aside
        [InlineData("Fastned (BE)", "Fastned Belgium")]           // a longer form of the same name
        [InlineData("Allego Charging (NL)", "Allego")]            // differs only by a generic term
        [InlineData("ChargePoint (US)", "Charge Point")]          // one word or two
        [InlineData("Ionity (FR)", "Ionety")]                     // a spelling variant
        public void Operators_which_may_be_the_same_are_flagged_for_review(string existingTitle, string submittedName)
        {
            Assert.True(OperatorInfoManager.IsSimilarName(existingTitle, submittedName));
        }

        [Theory]
        [InlineData("Electrify America (US)", "Electrify Canada")] // same brand, genuinely separate operators
        [InlineData("EV Connect (US)", "EVCS")]
        [InlineData("EVgo (US)", "EVBOLT")]
        [InlineData("EV Range (US)", "EV Start")]                  // alike only in their generic words
        [InlineData("EV Power UK (GB)", "EV Energy UK")]
        [InlineData("ChargePoint (US)", "ChargeUP")]
        [InlineData("Blink (US)", "Chargie")]
        public void Operators_which_merely_share_sector_wording_are_not_flagged(string existingTitle, string submittedName)
        {
            Assert.False(OperatorInfoManager.IsSimilarName(existingTitle, submittedName));
        }

        [Theory]
        [InlineData("ChargePoint (US)", "charge point")]      // punctuation and spacing are ignored
        [InlineData("ChargePoint (US)", "CHARGEPOINT")]       // case is ignored
        [InlineData("Electrify America (US)", "america")]     // matches anywhere in the name
        [InlineData("Ionity (DE)", "")]                       // an empty search matches everything
        public void Name_search_matches_on_the_operator_name(string title, string searchTerm)
        {
            Assert.True(OperatorInfoManager.MatchesNameSearch(title, searchTerm));
        }

        [Theory]
        [InlineData("Ionity (DE)", "fastned")]
        [InlineData("Ionity (DE)", "DE")]                     // the country code suffix is not searched
        public void Name_search_ignores_non_matching_names_and_the_country_suffix(string title, string searchTerm)
        {
            Assert.False(OperatorInfoManager.MatchesNameSearch(title, searchTerm));
        }
    }
}
