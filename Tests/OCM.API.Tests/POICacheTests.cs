using Microsoft.Extensions.Configuration;
using OCM.Core.Settings;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace OCM.API.Tests
{
    /// <summary>
    /// Range of tests on filter parameters for POI retrieval
    /// </summary>
    public class POICacheTests
    {
        private CoreSettings _settings = new CoreSettings();

        private IConfiguration GetConfiguration()
        {
            var configPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            return new ConfigurationBuilder()
                   .SetBasePath(configPath)
                   .AddJsonFile("appsettings.json", optional: true)
                   .AddUserSecrets("407b1e1a-5108-48b6-bb48-4990a0804269")
                   .Build();
        }
        public POICacheTests()
        {
            var config = GetConfiguration();
            var settings = new Core.Settings.CoreSettings();
            config.GetSection("CoreSettings").Bind(settings);
            _settings = settings;
            _ = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            Core.Data.CacheManager.InitCaching(_settings);

            _ = Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified).Result;
        }

        [Fact]
        public async Task CacheStatusOK()
        {

            var status = await Core.Data.CacheManager.GetCacheStatus();
            Assert.True(status != null, "Status should not be null");

            Assert.True(status.LastUpdated > DateTime.UtcNow.AddMinutes(-1), $"Status should be recently updated {status.LastUpdated} vs {DateTime.UtcNow}");
        }

        [Fact]
        public void ReturnPOIResults()
        {
            var api = new OCM.API.Common.POIManager();
            var results = api.GetPOIList(new Common.APIRequestParams { AllowMirrorDB = true, AllowDataStoreDB = false, IsVerboseOutput = false, MaxResults = 10 }); ;
            Assert.True(results.Count() > 0, "Results should be greater than 0");
            Assert.True(results.Count() == 10, "Results should be limited to 10");

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);
        }

        [Fact]
        public async Task UpdateCacheModified()
        {
            var result = await Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified).ConfigureAwait(true);

            Assert.True(result.StatusCode != System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Fact(Skip = "Manual")]
        public async Task UpdateCacheAll()
        {
            var result = await Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.All).ConfigureAwait(true);

            Assert.True(result.StatusCode != System.Net.HttpStatusCode.ExpectationFailed);
        }



        [Fact]
        public void ReturnReferenceData()
        {
            var api = new OCM.API.Common.ReferenceDataManager();
            var filter = new Common.APIRequestParams { AllowMirrorDB = true, AllowDataStoreDB = false, IsVerboseOutput = false };
            var results = api.GetCoreReferenceData(filter);

            Assert.True(results.Countries.Count > 0, "Country results should be greater than 0");
            Assert.True(results.ConnectionTypes.Count > 0, "Connection type results should be greater than 0");
            Assert.True(results.ChargePoint != null, "POI template object should not be null");
        }

        [Fact]
        public void ReturnFilteredReferenceData()
        {
            var api = new OCM.API.Common.ReferenceDataManager();
            var filter = new Common.APIRequestParams { CountryIDs = new int[] { 18 }, AllowMirrorDB = true, AllowDataStoreDB = false, IsVerboseOutput = false };
            var results = api.GetCoreReferenceData(filter);

            Assert.True(results.Countries.Count > 0, "Country results should be greater than 0");
            Assert.True(results.ConnectionTypes.Count > 0, "Connection type results should be greater than 0");
            Assert.True(results.ChargePoint != null, "POI template object should not be null");
        }
    }
}