using System;
using System.Collections.Generic;
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
            var api = new OCM.API.Common.POIManager();
            var results = api.GetChargePoints(new Common.APIRequestParams { ChargePointIDs = new int[] { 10000 } });
            Assert.Equal(results.Count, 1);

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);

            Assert.Equal(poi.AddressInfo.CountryID, 168); //Norway
        }
    }
}