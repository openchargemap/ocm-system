using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OCM.API.Tests
{
    public class TestSpatialAnalysis
    {
        [Fact]
        public void TestCountryPointMatch()
        {
            var analysis = new OCM.Import.Analysis.SpatialAnalysis();

            var r1 = analysis.ClassifyPoint(38.58431244, -121.4956055);
            Assert.Equal(r1.CountryCode, "US");

            var r2 = analysis.ClassifyPoint(57.142, -2.096);
            Assert.Equal(r2.CountryCode, "GB");

            var poiManager = new OCM.API.Common.POIManager();
            var list = poiManager.GetChargePoints(new Common.APIRequestParams { MaxResults = 100 });
            foreach (var poi in list)
            {
                var result = analysis.ClassifyPoint(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude);
                //Assert.NotNull(result);
                if (result == null)
                {
                    System.Diagnostics.Debug.WriteLine("Country Not Found (OCM-" + poi.ID + " - " + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude + ") :" + poi.AddressInfo.ToString());
                }
                else
                {
                    if (poi.AddressInfo.Country.ISOCode != result.CountryCode)
                    {
                        System.Diagnostics.Debug.WriteLine("Mismatched Country (" + result.CountryCode + " " + result.CountryName + "): OCM-" + poi.ID + " - " + poi.AddressInfo.ToString());
                    }
                }

                //Assert.Equal(poi.AddressInfo.Country.ISOCode, result.CountryCode);
            }
            //var r3 = analysis.ClassifyPoint(22.2492008209229, 114.14786529541);
            //Assert.Equal(r3.CountryCode, "ZH");
            System.Diagnostics.Debug.WriteLine("Processing Tests Completed");
        }
    }
}