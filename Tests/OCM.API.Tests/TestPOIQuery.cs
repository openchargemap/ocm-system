using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OCM.API.Tests
{
    /// <summary>
    /// Range of tests on filter parameters for POI retrieval
    /// </summary>
    public class TestPOIQuery
    {
        [Fact]
        public void FilterOnChargePointID()
        {

            var myConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;

            var api = new OCM.API.Common.POIManager();
            var results = api.GetPOIList(new Common.APIRequestParams { ChargePointIDs = new int[] { 10000 } });
            Assert.Equal(1, results.Count);

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);

            Assert.Equal(poi.AddressInfo.CountryID, 168); //Norway
        }

        [Fact]
        public void ReturnPOIResults()
        {

            //var myConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;

            var api = new OCM.API.Common.POIManager();
            var results = api.GetPOIList(new Common.APIRequestParams { EnableCaching=false, IsVerboseOutput=false });
            Assert.True(results.Count>0);

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);

        }
    }
}