using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using OCM.Core.Settings;
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

            var c = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            Core.Data.CacheManager.InitCaching(_settings);

        }
        [Fact]
        public async Task CacheStatusOK()
        {
            await Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified).ConfigureAwait(true);

            var status = await Core.Data.CacheManager.GetCacheStatus();
            Assert.True(status != null, "Status should not be null");

            Assert.True(status.LastUpdated > DateTime.UtcNow.AddMinutes(-1), $"Status should be recently updated {status.LastUpdated} vs {DateTime.UtcNow}");
        }

        [Fact]
        public void ReturnPOIResults()
        {
            var api = new OCM.API.Common.POIManager();
            var results = api.GetPOIList(new Common.APIRequestParams { AllowMirrorDB = true, AllowDataStoreDB = false, IsVerboseOutput = false, MaxResults = 10 }); ;
            Assert.True(results.Count > 0, "Results should be greater than 0");
            Assert.True(results.Count == 10, "Results should be limited to 10");

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);
        }

        [Fact]
        public async Task UpdateCacheModified()
        {
            var result = await Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified).ConfigureAwait(true);

            Assert.True(result.StatusCode != System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Fact(Skip="Manual")]
        public async Task UpdateCacheAll()
        {
            var result = await Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.All).ConfigureAwait(true);

            Assert.True(result.StatusCode != System.Net.HttpStatusCode.ExpectationFailed);
        }
    }
}