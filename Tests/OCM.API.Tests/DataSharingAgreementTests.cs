using System.Collections.Generic;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.Import.Providers.OCPI;
using Xunit;

namespace OCM.API.Tests
{
    public class DataSharingAgreementTests
    {
        private static readonly List<OperatorInfo> Operators = new List<OperatorInfo>
        {
            new OperatorInfo { ID = 1, Title = "(Unknown Operator)" },
            new OperatorInfo { ID = 10, Title = "Ionity (DE)" },
            new OperatorInfo { ID = 11, Title = "Ionity (FR)" },
            new OperatorInfo { ID = 20, Title = "Tesla" },
            new OperatorInfo { ID = 30, Title = "Volta Charging (NL)" },
            new OperatorInfo { ID = 40, Title = "Greenway" },
            new OperatorInfo { ID = 41, Title = "Greenway GmbH" }
        };

        [Theory]
        [InlineData("Tesla", "US", 20)]
        [InlineData("tesla", null, 20)]
        [InlineData("Ionity", "DE", 10)]
        [InlineData("IONITY", "FR", 11)]
        [InlineData("Ionity (FR)", "DE", 11)]
        [InlineData("Volta B.V.", "NL", 30)]
        public void Best_match_finds_the_operator_for_the_country(string name, string isoCode, int expectedId)
        {
            Assert.Equal(expectedId, OperatorInfoManager.FindBestMatch(Operators, name, isoCode)?.ID);
        }

        [Theory]
        [InlineData("Ionity", "ES")] // only listed for other countries
        [InlineData("Volta", "BE")]
        [InlineData("Greenway Ltd", "GB")] // two operators have this name, so neither is safe to pick
        [InlineData("Unknown Operator", "GB")]
        [InlineData("", "GB")]
        [InlineData(null, "GB")]
        public void Best_match_leaves_uncertain_names_unmatched(string name, string isoCode)
        {
            Assert.Null(OperatorInfoManager.FindBestMatch(Operators, name, isoCode));
        }

        [Fact]
        public void Provider_match_reports_name_and_website()
        {
            Assert.Equal(
                "has a similar name and the same website (example.com)",
                DataProviderManager.GetMatchReason("Example Network", "https://www.example.com/about", new[] { "Example Network Ltd" }, "http://example.com"));
        }

        [Fact]
        public void Provider_match_finds_a_shared_website_under_another_name()
        {
            Assert.Equal(
                "has the same website (example.com)",
                DataProviderManager.GetMatchReason("Open Chargers", "https://example.com", new[] { "Parent Holdings" }, "https://example.com"));
        }

        [Theory]
        [InlineData("Different Provider", "https://other.example.org", "Example Network", "https://example.com")]
        [InlineData("Different Provider", null, "Example Network", null)]
        public void Provider_match_ignores_unrelated_providers(string providerTitle, string providerWebsite, string title, string website)
        {
            Assert.Null(DataProviderManager.GetMatchReason(providerTitle, providerWebsite, new[] { title }, website));
        }

        [Fact]
        public void Saving_a_review_keeps_settings_which_are_not_on_the_form()
        {
            var stored = new OCPIProviderConfiguration
            {
                AppendAddressToTitle = true,
                IgnoredLocationTitles = new List<string> { "Acme Charging Station" }
            };

            var reviewed = new OCPIProviderConfiguration();
            reviewed.KeepSettingsNotReviewed(stored);

            Assert.True(reviewed.AppendAddressToTitle);
            Assert.Equal(new[] { "Acme Charging Station" }, reviewed.IgnoredLocationTitles);
        }

        [Fact]
        public void Saving_a_review_clears_operators_which_are_now_mapped()
        {
            var stored = new OCPIProviderConfiguration
            {
                NewOperatorsRequired = new List<string> { "Acme", "Other Operator" }
            };

            var reviewed = new OCPIProviderConfiguration
            {
                OperatorMappings = new Dictionary<string, int> { { "ACME", 123 } }
            };
            reviewed.KeepSettingsNotReviewed(stored);

            Assert.Equal(new[] { "Other Operator" }, reviewed.NewOperatorsRequired);
        }

        [Fact]
        public void Saving_a_review_with_no_stored_config_changes_nothing()
        {
            var reviewed = new OCPIProviderConfiguration();
            reviewed.KeepSettingsNotReviewed(null);

            Assert.False(reviewed.AppendAddressToTitle);
            Assert.Empty(reviewed.NewOperatorsRequired);
        }

        [Fact]
        public void New_operators_recorded_by_a_submission_are_read_back_from_the_stored_config()
        {
            var config = JsonConvert.DeserializeObject<OCPIProviderConfiguration>(@"{ ""NewOperatorsRequired"": [ ""Acme"" ] }");

            Assert.Equal(new[] { "Acme" }, config.NewOperatorsRequired);
        }
    }
}
