using Xunit;

namespace OCM.API.Tests
{
    public class TestSpatialAnalysis
    {
        [Fact]
        public void TestCountryPointMatch()
        {
            var analysis = new OCM.Import.Analysis.SpatialAnalysis(@"C:\Temp\ocm\data\import\Shapefiles\World\ne_10m_admin_0_map_units.shp");

            var r1 = analysis.ClassifyPoint(38.58431244, -121.4956055);
            Assert.Equal("US", r1.CountryCode);

            var r2 = analysis.ClassifyPoint(57.142, -2.096);
            Assert.Equal("GB", r2.CountryCode);

            var poiManager = new OCM.API.Common.POIManager();
            var list = poiManager.GetPOIList(new Common.APIRequestParams { MaxResults = 100 });
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