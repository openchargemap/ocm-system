using Newtonsoft.Json;
using OCM.API.Common;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
            var poiResults = adapter.FromOCPI(ocpiList, 0);

            Assert.Single(poiResults);
        }

        [Fact]
        async Task CanConvertFromOCPI_Fastned()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_1_1_locations-fastned.json");

            var refDataManager = new ReferenceDataManager();
            var coreRefData = await refDataManager.GetCoreReferenceDataAsync(new APIRequestParams());

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(coreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OCM.Model.OCPI.LocationsResponse>(json);
            var ocpiData = response.Data;

            var poiResults = adapter.FromOCPI(ocpiData, 0);

            Assert.Equal(ocpiData.Count, poiResults.Count());

            Assert.Equal("NLD", ocpiData.First().Country);
            Assert.Equal(159, poiResults.First().AddressInfo.CountryID);
            Assert.Equal(33, poiResults.First().Connections.First().ConnectionTypeID);
        }

        [Fact]
        async Task CanConvertFromOCPI_EVIO()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-evio.json");

            var refDataManager = new ReferenceDataManager();
            var coreRefData = await refDataManager.GetCoreReferenceDataAsync(new APIRequestParams());

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(coreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OCM.Model.OCPI.Location>>(json);
            var poiResults = adapter.FromOCPI(response, 30);

            Assert.Equal(21, poiResults.Count());

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(poiResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        [Fact]
        async Task CanConvertFromOCPI_MobiePt()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-mobie.pt.json");

            var refDataManager = new ReferenceDataManager();
            var coreRefData = await refDataManager.GetCoreReferenceDataAsync(new APIRequestParams());

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(coreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OCM.Model.OCPI.Location>>(json);
            var poiResults = adapter.FromOCPI(response, 7).ToList();

            Assert.Equal(3299, poiResults.Count());

            // ensure power KW does not exceed a reasonable value
            Assert.Empty(poiResults.Where(p => p.Connections.Any(c => c.PowerKW > 2000)));

            var output = JsonConvert.SerializeObject(poiResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            System.IO.File.WriteAllText("C:\\temp\\ocm\\ocpi.json", output);
        }
    }
}
