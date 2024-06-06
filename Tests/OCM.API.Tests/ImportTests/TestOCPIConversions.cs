using Newtonsoft.Json;
using OCM.API.Common;
using OCM.Import.Providers.OCPI;
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
        async Task CanConvertFromOCPI_Sitronics()
        {

            var parseErrors = 0;
            var deserializeSettings = new JsonSerializerSettings
            {
                Error = (obj, args) =>
                {
                    var contextErrors = args.ErrorContext;
                    contextErrors.Handled = true;

                    System.Console.WriteLine($"Error parsing item {contextErrors.Error}");
                    parseErrors++;
                }
            };


            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_1_locations-sitronics.json");

            var refDataManager = new ReferenceDataManager();
            var coreRefData = await refDataManager.GetCoreReferenceDataAsync(new APIRequestParams());

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(coreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OCM.Model.OCPI.LocationsResponse>(json, deserializeSettings);
            var ocpiData = response.Data;
            var poiResults = adapter.FromOCPI(ocpiData, 31);

            Assert.Equal(65, poiResults.Count());

            Assert.Equal(0, parseErrors);

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(poiResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }


        [Fact]
        async Task CanConvertFromOCPI_Toger()
        {

            var parseErrors = 0;
            var deserializeSettings = new JsonSerializerSettings
            {
                Error = (obj, args) =>
                {
                    var contextErrors = args.ErrorContext;
                    contextErrors.Handled = true;

                    System.Console.WriteLine($"Error parsing item {contextErrors.Error}");
                    parseErrors++;
                }
            };


            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_1_locations-toger.json");

            var refDataManager = new ReferenceDataManager();
            var coreRefData = await refDataManager.GetCoreReferenceDataAsync(new APIRequestParams());

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(coreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OCM.Model.OCPI.LocationsResponse>(json, deserializeSettings);
            var ocpiData = response.Data;
            var poiResults = adapter.FromOCPI(ocpiData, 34);

            Assert.Equal(41, poiResults.Count());

            Assert.Equal(0, parseErrors);

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(poiResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }


        [Fact]
        async Task CanConvertFromOCPI_Gaia()
        {

            var parseErrors = 0;
            var deserializeSettings = new JsonSerializerSettings
            {
                Error = (obj, args) =>
                {
                    var contextErrors = args.ErrorContext;
                    contextErrors.Handled = true;

                    System.Diagnostics.Debug.WriteLine($"Error parsing item {contextErrors.Error}");
                    parseErrors++;
                }
            };


            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-gaia.json");

            var refDataManager = new ReferenceDataManager();
            var coreRefData = await refDataManager.GetCoreReferenceDataAsync(new APIRequestParams());

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(coreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OCM.Model.OCPI.LocationsResponse>(json, deserializeSettings);
            var ocpiData = response.Data;
            var poiResults = adapter.FromOCPI(ocpiData, 33);

            Assert.Equal(4, poiResults.Count());

            Assert.Equal(0, parseErrors);

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(poiResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }
        [Fact]
        async Task CanConvertFromOCPI_ElectricEra()
        {

            var parseErrors = 0;
            var deserializeSettings = new JsonSerializerSettings
            {
                Error = (obj, args) =>
                {
                    var contextErrors = args.ErrorContext;
                    contextErrors.Handled = true;

                    System.Console.WriteLine($"Error parsing item {contextErrors.Error}");
                    parseErrors++;
                }
            };


            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-electricera.json");

            var refDataManager = new ReferenceDataManager();
            var coreRefData = await refDataManager.GetCoreReferenceDataAsync(new APIRequestParams());

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(coreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OCM.Model.OCPI.LocationsResponse>(json, deserializeSettings);
            var ocpiData = response.Data;
            var poiResults = adapter.FromOCPI(ocpiData, 35);

            Assert.Equal(3, poiResults.Count());

            Assert.Equal(0, parseErrors);

            var importProvider = new ImportProvider_ElectricEra();
            importProvider.InputData = json;

            var importPreview = importProvider.Process(coreRefData);

            Assert.Equal(poiResults.Count(), importPreview.Count());

            Assert.True(importPreview.Any(p => p.DataProviderID != 35) == false);
            Assert.True(importPreview.Any(p => p.OperatorID != 3789) == false);

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(importPreview, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        [Fact]
        async Task CanConvertFromOCPI_MobiePt()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-mobie.pt.json");

            var refDataManager = new ReferenceDataManager();
            var coreRefData = await refDataManager.GetCoreReferenceDataAsync(new APIRequestParams());

            var adapter = new ImportProvider_Mobie();
            adapter.InputData = json;

            var poiResults = adapter.Process(coreRefData).ToList();

            Assert.Equal(3299, poiResults.Count());

            var unmappedOperators = adapter.GetPostProcessingUnmappedOperators();

            foreach (var o in unmappedOperators.OrderByDescending(i => i.Value))
            {
                System.Diagnostics.Debug.WriteLine($"Unmapped Operator: {o.Key} {o.Value}");
            }

            Assert.False(unmappedOperators.Any(o => o.Value > 50), $"Should not proceed if unknown operator has more than 50 POIs: [{unmappedOperators.FirstOrDefault(u => u.Value > 50).Key}]");

            // ensure power KW does not exceed a reasonable value
            Assert.Empty(poiResults.Where(p => p.Connections.Any(c => c.PowerKW > 2000)));

            var output = JsonConvert.SerializeObject(poiResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            System.IO.File.WriteAllText("C:\\temp\\ocm\\ocpi.json", output);
        }

        [Fact]
        async Task CanConvertFromOCPI_Lakd()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-lakd.lt.json");

            var refDataManager = new ReferenceDataManager();
            var coreRefData = await refDataManager.GetCoreReferenceDataAsync(new APIRequestParams());

            var adapter = new ImportProvider_Lakd();

            json = json.Replace("\"location\"", "\"coordinates\"");
            adapter.InputData = json;

            var poiResults = adapter.Process(coreRefData).ToList();

            Assert.Equal(591, poiResults.Count());

            var unmappedOperators = adapter.GetPostProcessingUnmappedOperators();

            foreach (var o in unmappedOperators.OrderByDescending(i => i.Value))
            {
                System.Diagnostics.Debug.WriteLine($"Unmapped Operator: {o.Key} {o.Value}");
            }

            Assert.False(unmappedOperators.Any(o => o.Value > 50), $"Should not proceed if unknown operator has more than 50 POIs: [{unmappedOperators.FirstOrDefault(u => u.Value > 50).Key}]");


            // ensure power KW does not exceed a reasonable value
            var powerfulPOIs = poiResults.Where(p => p.Connections.Any(c => c.PowerKW > 2000));

            Assert.Empty(powerfulPOIs);
        }
    }
}
