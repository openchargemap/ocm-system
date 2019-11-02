using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OCM.API.Common;
using Xunit;

namespace OCM.API.Tests
{
    public class TestOCPIConversions
    {
        [Fact]
        void CanConvertFromOCPI_Example()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_3_0_0_location_example.json");

            var refDataManager = new ReferenceDataManager();
            var coreRefData = refDataManager.GetCoreReferenceData(new APIRequestParams());

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(coreRefData);

            var ocpiList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OCM.Model.OCPI.Location>>(json);
            var poiResults = adapter.FromOCPI(ocpiList);

            Assert.Single(poiResults);
        }

        [Fact]
        void CanConvertFromOCPI_Fastned()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_1_1_locations-fastned.json");

            var refDataManager = new ReferenceDataManager();
            var coreRefData = refDataManager.GetCoreReferenceData(new APIRequestParams());

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(coreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OCM.Model.OCPI.LocationsResponse>(json);
            var poiResults = adapter.FromOCPI(response.Data);

            Assert.Equal(106, poiResults.Count());
        }
    }
}
