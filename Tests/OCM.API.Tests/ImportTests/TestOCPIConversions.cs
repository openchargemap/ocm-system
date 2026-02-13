using Newtonsoft.Json;
using OCM.API.Client;
using OCM.API.Common.Model;
using OCM.Import.Providers.OCPI;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OCM.API.Tests.ImportTests
{
    public class TestOCPIConversions : IAsyncLifetime
    {
        private const string API_BASE_URL = "https://api.openchargemap.io/v3";
        private static CoreReferenceData _coreRefData;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static bool _isInitialized = false;

        /// <summary>
        /// Initialize shared test data - fetches CoreReferenceData from the OCM API once for all tests
        /// </summary>
        public async Task InitializeAsync()
        {
            await EnsureInitializedAsync();
        }

        /// <summary>
        /// Ensures CoreReferenceData is initialized - can be called from sync or async context
        /// </summary>
        private static async Task EnsureInitializedAsync()
        {
            if (_isInitialized && _coreRefData != null)
            {
                return;
            }

            await _semaphore.WaitAsync();
            try
            {
                if (!_isInitialized)
                {
                    // Fetch core reference data from the live API using OCMClient
                    using var client = new OCMClient(API_BASE_URL, apiKey: "statuscake");
                    _coreRefData = await client.GetCoreReferenceDataAsync();
                    _isInitialized = true;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Synchronous wrapper for initialization - used by sync tests
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_isInitialized && _coreRefData != null)
            {
                return;
            }

            EnsureInitializedAsync().GetAwaiter().GetResult();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>
        /// Gets the shared CoreReferenceData instance, ensuring it's initialized
        /// </summary>
        private CoreReferenceData CoreRefData
        {
            get
            {
                EnsureInitialized();

                // Assert that we have valid reference data - tests will fail with clear message if API is not reachable
                Assert.NotNull(_coreRefData);
                Assert.NotNull(_coreRefData.Countries);
                Assert.NotEmpty(_coreRefData.Countries);

                return _coreRefData;
            }
        }

        [Fact]
        public void CanCreateConfigurableOCPIProvider()
        {
            // Arrange - create a configuration
            var config = new OCPIProviderConfiguration
            {
                ProviderName = "test-provider",
                OutputNamePrefix = "test",
                Description = "Test configurable provider",
                DataProviderId = 35,
                LocationsEndpointUrl = "https://example.com/ocpi/2.2/locations",
                AuthHeaderKey = null,
                CredentialKey = null,
                DefaultOperatorId = 3789,
                IsAutoRefreshed = true,
                IsProductionReady = true,
                IsEnabled = true,
                AllowDuplicatePOIWithDifferentOperator = true,
                OperatorMappings = new Dictionary<string, int>
                {
                    { "Test Operator", 3789 }
                },
                ExcludedLocationIds = new List<string> { "LOC001" }
            };

            // Act - create the provider
            var provider = new ImportProvider_OCPIConfigurable(config);

            // Assert
            Assert.Equal("test-provider", provider.GetProviderName());
            Assert.Equal(35, provider.DataProviderID);
            Assert.Equal(3789, provider.DefaultOperatorID);
            Assert.True(provider.IsAutoRefreshed);
            Assert.True(provider.IsProductionReady);
            Assert.Contains("LOC001", provider.ExcludedLocations);

            var mappings = provider.GetOperatorMappings();
            Assert.Single(mappings);
            Assert.Equal(3789, mappings["Test Operator"]);
        }

        [Fact]
        public void CanLoadProvidersFromJsonConfiguration()
        {
            // Arrange - create JSON configuration
            var jsonConfig = @"{
                ""Providers"": [
                    {
                        ""ProviderName"": ""config-test-1"",
                        ""DataProviderId"": 35,
                        ""LocationsEndpointUrl"": ""https://example1.com/ocpi/2.2/locations"",
                        ""DefaultOperatorId"": 3789,
                        ""IsEnabled"": true
                    },
                    {
                        ""ProviderName"": ""config-test-2"",
                        ""DataProviderId"": 31,
                        ""LocationsEndpointUrl"": ""https://example2.com/ocpi/2.2/locations"",
                        ""CredentialKey"": ""OCPI-TEST"",
                        ""IsEnabled"": true
                    },
                    {
                        ""ProviderName"": ""config-test-disabled"",
                        ""DataProviderId"": 99,
                        ""LocationsEndpointUrl"": ""https://example3.com/ocpi/2.2/locations"",
                        ""IsEnabled"": false
                    }
                ]
            }";

            // Act
            var loader = new OCPIProviderLoader();
            var loadResult = loader.LoadFromJson(jsonConfig);
            var providers = loader.CreateProviders(enabledOnly: true);

            // Assert
            Assert.True(loadResult);
            Assert.Equal(2, providers.Count); // Only 2 enabled providers
            Assert.Contains(providers, p => p.GetProviderName() == "config-test-1");
            Assert.Contains(providers, p => p.GetProviderName() == "config-test-2");
            Assert.DoesNotContain(providers, p => p.GetProviderName() == "config-test-disabled");
        }

        [Fact]
        public void OCPIProviderLoaderValidatesConfiguration()
        {
            // Arrange - configuration with missing required fields
            var jsonConfig = @"{
                ""Providers"": [
                    {
                        ""ProviderName"": """",
                        ""DataProviderId"": 35,
                        ""LocationsEndpointUrl"": ""https://example.com/ocpi/2.2/locations""
                    },
                    {
                        ""ProviderName"": ""missing-url"",
                        ""DataProviderId"": 35,
                        ""LocationsEndpointUrl"": """"
                    },
                    {
                        ""ProviderName"": ""invalid-dataprovider"",
                        ""DataProviderId"": 0,
                        ""LocationsEndpointUrl"": ""https://example.com/ocpi/2.2/locations""
                    },
                    {
                        ""ProviderName"": ""valid-provider"",
                        ""DataProviderId"": 35,
                        ""LocationsEndpointUrl"": ""https://example.com/ocpi/2.2/locations""
                    }
                ]
            }";

            // Act
            var loader = new OCPIProviderLoader();
            loader.LoadFromJson(jsonConfig);
            var providers = loader.CreateProviders();

            // Assert - only the valid provider should be created
            Assert.Single(providers);
            Assert.Equal("valid-provider", providers.First().GetProviderName());
        }

        [Fact]
        public void CanProcessDataWithConfigurableProvider()
        {
            // Arrange - load test data
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-electricera.json");

            // Create a configurable provider that matches the ElectricEra settings
            var config = new OCPIProviderConfiguration
            {
                ProviderName = "electricera-configurable",
                DataProviderId = 35,
                LocationsEndpointUrl = "https://ocpi-http.app.electricera.tech/ocpi/2.2/locations",
                DefaultOperatorId = 3789,
                IsProductionReady = true,
                OperatorMappings = new Dictionary<string, int>
                {
                    { "Electric Era", 3789 }
                }
            };

            var configurableProvider = new ImportProvider_OCPIConfigurable(config);
            configurableProvider.InputData = json;

            // Act
            var configurableResults = configurableProvider.Process(CoreRefData);

            // Assert
            Assert.Equal(3, configurableResults.Count);

            // Verify data provider and operator IDs
            Assert.True(configurableResults.All(p => p.DataProviderID == 35));
            Assert.True(configurableResults.All(p => p.OperatorID == 3789));
        }

        [Fact]
        public void CanGenerateExampleConfiguration()
        {
            // Act
            var exampleJson = OCPIProviderLoader.GenerateExampleConfiguration();

            // Assert
            Assert.NotNull(exampleJson);
            Assert.Contains("example-provider", exampleJson);
            Assert.Contains("LocationsEndpointUrl", exampleJson);
            Assert.Contains("OperatorMappings", exampleJson);

            // Verify it can be parsed back
            var loader = new OCPIProviderLoader();
            var loadResult = loader.LoadFromJson(exampleJson);
            Assert.True(loadResult);
        }

        [Fact]
        public void CanConvertFromOCPI_Example()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_3_0_0_location_example.json");

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(CoreRefData);

            var ocpiList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OCM.Model.OCPI.Location>>(json);
            var poiResults = adapter.FromOCPI(ocpiList, 0);

            Assert.Single(poiResults);
        }

        [Fact]
        public void CanConvertFromOCPI_Fastned()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_1_1_locations-fastned.json");

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(CoreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OCM.Model.OCPI.LocationsResponse>(json);
            var ocpiData = response.Data;

            var poiResults = adapter.FromOCPI(ocpiData, 0);

            Assert.Equal(ocpiData.Count, poiResults.Count());

            Assert.Equal("NLD", ocpiData.First().Country);
            Assert.Equal(159, poiResults.First().AddressInfo.CountryID);
            Assert.Equal(33, poiResults.First().Connections.First().ConnectionTypeID);
        }

        [Fact]
        public void CanConvertFromOCPI_EVIO()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-evio.json");

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(CoreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OCM.Model.OCPI.Location>>(json);
            var poiResults = adapter.FromOCPI(response, 30);

            Assert.Equal(21, poiResults.Count());

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(poiResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }


        [Fact]
        public void CanConvertFromOCPI_Sitronics()
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

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(CoreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OCM.Model.OCPI.LocationsResponse>(json, deserializeSettings);
            var ocpiData = response.Data;
            var poiResults = adapter.FromOCPI(ocpiData, 31);

            Assert.Equal(65, poiResults.Count());

            Assert.Equal(0, parseErrors);

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(poiResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }


        [Fact]
        public void CanConvertFromOCPI_Toger()
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

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(CoreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OCM.Model.OCPI.LocationsResponse>(json, deserializeSettings);
            var ocpiData = response.Data;
            var poiResults = adapter.FromOCPI(ocpiData, 34);

            Assert.Equal(41, poiResults.Count());

            Assert.Equal(0, parseErrors);

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(poiResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }


        [Fact]
        public void CanConvertFromOCPI_Gaia()
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

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(CoreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OCM.Model.OCPI.LocationsResponse>(json, deserializeSettings);
            var ocpiData = response.Data;
            var poiResults = adapter.FromOCPI(ocpiData, 33);

            Assert.Equal(4, poiResults.Count());

            Assert.Equal(0, parseErrors);

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(poiResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }
        [Fact]
        public void CanConvertFromOCPI_ElectricEra()
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

            var adapter = new Common.Model.OCPI.OCPIDataAdapter(CoreRefData);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<OCM.Model.OCPI.LocationsResponse>(json, deserializeSettings);
            var ocpiData = response.Data;
            var poiResults = adapter.FromOCPI(ocpiData, 35);

            Assert.Equal(3, poiResults.Count());

            Assert.Equal(0, parseErrors);

            var importProvider = new ImportProvider_OCPIConfigurable(new OCPIProviderConfiguration
            {
                ProviderName = "electricera",
                DataProviderId = 35,
                LocationsEndpointUrl = "https://ocpi-http.app.electricera.tech/ocpi/2.2/locations",
                DefaultOperatorId = 3789,
                IsProductionReady = true,
                OperatorMappings = new Dictionary<string, int> { { "Electric Era", 3789 } }
            });
            importProvider.InputData = json;

            var importPreview = importProvider.Process(CoreRefData);

            Assert.Equal(poiResults.Count(), importPreview.Count());

            Assert.True(importPreview.Any(p => p.DataProviderID != 35) == false);
            Assert.True(importPreview.Any(p => p.OperatorID != 3789) == false);

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(importPreview, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        [Fact]
        public void CanConvertFromOCPI_MobiePt()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-mobie.pt.json");

            var adapter = new ImportProvider_OCPIConfigurable(new OCPIProviderConfiguration
            {
                ProviderName = "mobie.pt",
                OutputNamePrefix = "mobie",
                DataProviderId = 7,
                LocationsEndpointUrl = "https://ocpi.mobinteli.com/2.2/locations",
                IsProductionReady = true,
                OperatorMappings = new Dictionary<string, int>
                {
                    { "EDP", 3276 }, { "GLP", 3557 }, { "HRZ", 3550 }, { "GLG", 3557 },
                    { "MAK", 3649 }, { "MLT", 3557 }, { "REP", 91 }, { "IBD", 2247 },
                    { "PIR", 200 }, { "PRI", 200 }, { "HLX", 3645 }, { "EML", 2247 },
                    { "ION", 3299 }, { "MOO", 3644 }, { "EVP", 3643 }, { "GRC", 3648 },
                    { "MOB", 21 }, { "KLS", 3588 }, { "KLC", 3588 }, { "CPS", 3584 },
                    { "NRG", 3646 }, { "ECI", 3647 }, { "MOT", 3684 }, { "FAC", 3685 },
                    { "EVC", 3693 }
                }
            });
            adapter.InputData = json;

            var poiResults = adapter.Process(CoreRefData).ToList();

            Assert.Equal(3299, poiResults.Count());

            var unmappedOperators = adapter.GetPostProcessingUnmappedOperators();

            foreach (var o in unmappedOperators.OrderByDescending(i => i.Value))
            {
                System.Diagnostics.Debug.WriteLine($"Unmapped Operator: {o.Key} {o.Value}");
            }

            Assert.False(unmappedOperators.Any(o => o.Value > 50), $"Should not proceed if unknown operator has more than 50 POIs: [{unmappedOperators.FirstOrDefault(u => u.Value > 50).Key}]");

            // ensure power KW does not exceed a reasonable value
            Assert.Empty(poiResults.Where(p => p.Connections.Any(c => c.PowerKW > 2000)));

        }

        [Fact]
        public void CanConvertFromOCPI_Lakd()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-lakd.lt.json");

            var adapter = new ImportProvider_OCPIConfigurable(new OCPIProviderConfiguration
            {
                ProviderName = "lakd.lt",
                DataProviderId = 32,
                LocationsEndpointUrl = "https://ev.lakd.lt/ocpi/2.2.1/locations",
                IsProductionReady = true,
                OperatorMappings = new Dictionary<string, int>
                {
                    { "Ignitis UAB", 3688 }, { "UAB \u201EStova\u201C", 3689 },
                    { "In Balance grid, UAB", 3690 }, { "AB Lietuvos automobili\u0173 keli\u0173 direkcija", 3691 },
                    { "Eldrive Lithuania, UAB", 3692 }, { "IONITY GmbH", 3299 },
                    { "Lidl Lietuva", 38 }, { "UAB Enefit", 3862 }, { "Stuart Energy", 3877 }
                }
            });

            adapter.InputData = json;

            var poiResults = adapter.Process(CoreRefData).ToList();

            Assert.Equal(1235, poiResults.Count());

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

        [Fact]
        public void CanConvertFromOCPI_ITCharge()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_1_locations-itcharge.json");

            var adapter = new ImportProvider_OCPIConfigurable(new OCPIProviderConfiguration
            {
                ProviderName = "itcharge.ru",
                DataProviderId = 36,
                LocationsEndpointUrl = "https://ocpi.itcharge.ru/cpo/2.2.1/locations/",
                CredentialKey = "OCPI-ITCHARGE-CRED",
                DefaultOperatorId = 3650,
                IsProductionReady = true,
                OperatorMappings = new Dictionary<string, int> { { "ITC", 3650 } },
                ExcludedLocationIds = new List<string> { "c007dc86-07e1-449e-add9-efa44e9e46e8", "2a46e8c0-432d-41e2-810a-b0d3a348861e", "41c38115-4bdf-4ad9-8e1c-3863e13a9cff", "03c7019c-7e0e-4c40-8d45-3199e9d954d4" }
            });

            adapter.InputData = json;

            var poiResults = adapter.Process(CoreRefData).ToList();

            Assert.Equal(1180, poiResults.Count());

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

        [Fact]
        public void CanConvertFromOCPI_Voltrelli()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_locations-voltrelli.json");

            var adapter = new ImportProvider_OCPIConfigurable(new OCPIProviderConfiguration
            {
                ProviderName = "voltrelli",
                DataProviderId = 37,
                LocationsEndpointUrl = "https://api.evozone.app/api/ocpi/v1/charging-station/list",
                CredentialKey = "OCPI-VOLTRELLI",
                DefaultOperatorId = 3843,
                IsProductionReady = true,
                OperatorMappings = new Dictionary<string, int> { { "Voltrelli - Evconnect", 3843 } }
            });

            adapter.InputData = json;

            var poiResults = adapter.Process(CoreRefData).ToList();

            Assert.Equal(307, poiResults.Count());

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

        [Fact]
        public void CanConvertFromOCPI_PowerGO()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations_powergo.json");

            var adapter = new ImportProvider_OCPIConfigurable(new OCPIProviderConfiguration
            {
                ProviderName = "powergo",
                DataProviderId = 38,
                LocationsEndpointUrl = "https://powerops.powerfield.nl/ocpi/cpo/2.2/locations",
                CredentialKey = "OCPI-POWERGO",
                DefaultOperatorId = 3632,
                IsProductionReady = true,
                OperatorMappings = new Dictionary<string, int> { { "PFG", 3632 } }
            });

            adapter.InputData = json;

            var poiResults = adapter.Process(CoreRefData).ToList();

            Assert.Equal(489, poiResults.Count());


            // check that we have POIs in both Norway and Netherlands
            var countries = poiResults.Select(p => p.AddressInfo.CountryID).Distinct().ToList();

            Assert.Contains(159, countries); // Netherlands
            Assert.Contains(168, countries); // Norway

            var unmappedOperators = adapter.GetPostProcessingUnmappedOperators();

            foreach (var o in unmappedOperators.OrderByDescending(i => i.Value))
            {
                System.Diagnostics.Debug.WriteLine($"Unmapped Operator: {o.Key} {o.Value}");
            }

            // ensure power KW does not exceed a reasonable value
            var powerfulPOIs = poiResults.Where(p => p.Connections.Any(c => c.PowerKW > 2000));

            Assert.Empty(powerfulPOIs);
        }

        [Fact]
        public void CanConvertFromOCPI_PUnkt()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_1_locations-punkt.json");

            var adapter = new ImportProvider_OCPIConfigurable(new OCPIProviderConfiguration
            {
                ProviderName = "punkt-e",
                DataProviderId = 39,
                LocationsEndpointUrl = "https://io.api.punkt-e.io/2.2.1/cpo/locations",
                CredentialKey = "OCPI-PUNKT",
                DefaultOperatorId = 3636,
                IsProductionReady = true,
                OperatorMappings = new Dictionary<string, int> { { "TBM", 3634 } }
            });

            adapter.InputData = json;

            var poiResults = adapter.Process(CoreRefData).ToList();

            Assert.Equal(489, poiResults.Count());

            var unmappedOperators = adapter.GetPostProcessingUnmappedOperators();

            foreach (var o in unmappedOperators.OrderByDescending(i => i.Value))
            {
                System.Diagnostics.Debug.WriteLine($"Unmapped Operator: {o.Key} {o.Value}");
            }

            // ensure power KW does not exceed a reasonable value
            var powerfulPOIs = poiResults.Where(p => p.Connections.Any(c => c.PowerKW > 2000));

            Assert.Empty(powerfulPOIs);
        }

        [Fact]
        public void CanConvertFromOCPI_EzVolt()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_1_locations-ezvolt.json");

            var adapter = new ImportProvider_OCPIConfigurable(new OCPIProviderConfiguration
            {
                ProviderName = "ezvolt.com.br",
                DataProviderId = 40,
                LocationsEndpointUrl = "https://api3.mycharge.com.br/ocpi/2.2.1/locations",
                CredentialKey = "OCPI-EZVOLT",
                DefaultOperatorId = 3564,
                IsProductionReady = true,
                OperatorMappings = new Dictionary<string, int> { { "EZVolt", 3564 } }
            });

            adapter.InputData = json;

            var poiResults = adapter.Process(CoreRefData).ToList();

            Assert.Equal(91, poiResults.Count());

            var unmappedOperators = adapter.GetPostProcessingUnmappedOperators();

            foreach (var o in unmappedOperators.OrderByDescending(i => i.Value))
            {
                System.Diagnostics.Debug.WriteLine($"Unmapped Operator: {o.Key} {o.Value}");
            }

            // ensure power KW does not exceed a reasonable value
            var powerfulPOIs = poiResults.Where(p => p.Connections.Any(c => c.PowerKW > 2000));

            Assert.Empty(powerfulPOIs);
        }

        [Fact]
        public void CanConvertFromOCPI_Chargesini()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-chargesini.json");

            var adapter = new ImportProvider_Chargesini();

            adapter.InputData = json;

            var poiResults = adapter.Process(CoreRefData).ToList();

            Assert.Equal(448, poiResults.Count);

            var unmappedOperators = adapter.GetPostProcessingUnmappedOperators();

            foreach (var o in unmappedOperators.OrderByDescending(i => i.Value))
            {
                System.Diagnostics.Debug.WriteLine($"Unmapped Operator: {o.Key} {o.Value}");
            }

            // ensure power KW does not exceed a reasonable value
            var powerfulPOIs = poiResults.Where(p => p.Connections.Any(c => c.PowerKW > 2000));

            Assert.Empty(powerfulPOIs);
        }

        [Fact]
        public void CanConvertFromOCPI_Otopriz()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-otopriz.json");

            var adapter = new ImportProvider_OCPIConfigurable(new OCPIProviderConfiguration
            {
                ProviderName = "otopriz.com.tr",
                DataProviderId = 42,
                LocationsEndpointUrl = "https://otopriz.mapcontentpartners.service.electroop.io/2.2/locations",
                DefaultOperatorId = 3807,
                IsProductionReady = true,
                OperatorMappings = new Dictionary<string, int> { { "OTO", 3807 } }
            });

            adapter.InputData = json;

            var poiResults = adapter.Process(CoreRefData).ToList();

            Assert.Equal(500, poiResults.Count);

            var unmappedOperators = adapter.GetPostProcessingUnmappedOperators();

            foreach (var o in unmappedOperators.OrderByDescending(i => i.Value))
            {
                System.Diagnostics.Debug.WriteLine($"Unmapped Operator: {o.Key} {o.Value}");
            }

            // ensure power KW does not exceed a reasonable value
            var powerfulPOIs = poiResults.Where(p => p.Connections.Any(c => c.PowerKW > 2000));

            Assert.Empty(powerfulPOIs);
        }

        [Fact]
        public void CanConvertFromOCPI_Zepto_UsingConfigurableProvider()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_locations-zepto.json");

            // Create a configurable provider with the same settings as the original ImportProvider_Zepto
            var config = new OCPIProviderConfiguration
            {
                ProviderName = "zepto.pl",
                OutputNamePrefix = "zepto.pl",
                Description = "Zepto EV charging network - Poland",
                DataProviderId = 44,
                LocationsEndpointUrl = "https://zepto.pl/ocpi/cpo/2.2/",
                DefaultOperatorId = 3475,
                IsAutoRefreshed = true,
                IsProductionReady = true,
                IsEnabled = true,
                OperatorMappings = new Dictionary<string, int>
                {
                    { "ZEPTO", 3475 }
                }
            };

            var adapter = new ImportProvider_OCPIConfigurable(config);
            adapter.InputData = json;

            var poiResults = adapter.Process(CoreRefData).ToList();

            Assert.Equal(10, poiResults.Count);

            var unmappedOperators = adapter.GetPostProcessingUnmappedOperators();

            foreach (var o in unmappedOperators.OrderByDescending(i => i.Value))
            {
                System.Diagnostics.Debug.WriteLine($"Unmapped Operator: {o.Key} {o.Value}");
            }

            // ensure power KW does not exceed a reasonable value
            var powerfulPOIs = poiResults.Where(p => p.Connections.Any(c => c.PowerKW > 2000));

            Assert.Empty(powerfulPOIs);

            // Verify all POIs have correct data provider ID
            Assert.True(poiResults.All(p => p.DataProviderID == 44));

            // Verify all POIs have correct operator ID
            Assert.True(poiResults.All(p => p.OperatorID == 3475));

            // Verify all POIs are in Poland (Country ID 179)
            Assert.True(poiResults.All(p => p.AddressInfo.CountryID == 179));
        }

        [Fact]
        public void CanConvertFromOCPI_Greems()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_2_2_1_locations-greems.json");

            var adapter = new ImportProvider_OCPIConfigurable(new OCPIProviderConfiguration
            {
                ProviderName = "greems.io",
                DataProviderId = 45,
                LocationsEndpointUrl = "https://cpo.ocpi.cpo.greems.io/ocpi/cpo/2.2.1/locations",
                CredentialKey = "OCPI-GREEMS",
                DefaultOperatorId = 3899,
                IsProductionReady = true,
                OperatorMappings = new Dictionary<string, int> { { "GRE", 3899 } }
            });

            adapter.InputData = json;

            var poiResults = adapter.Process(CoreRefData).ToList();

            Assert.Equal(80, poiResults.Count);

            var unmappedOperators = adapter.GetPostProcessingUnmappedOperators();

            foreach (var o in unmappedOperators.OrderByDescending(i => i.Value))
            {
                System.Diagnostics.Debug.WriteLine($"Unmapped Operator: {o.Key} {o.Value}");
            }

            // ensure power KW does not exceed a reasonable value
            var powerfulPOIs = poiResults.Where(p => p.Connections.Any(c => c.PowerKW > 2000));

            Assert.Empty(powerfulPOIs);

            // Verify all POIs have correct data provider ID
            Assert.True(poiResults.All(p => p.DataProviderID == 45));

            // Verify all POIs have correct operator ID
            Assert.True(poiResults.All(p => p.OperatorID == 3899));
        }
    }
}
