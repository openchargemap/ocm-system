using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OCM.Import.Providers.OCPI
{
    /// <summary>
    /// Loads and manages configurable OCPI import providers from configuration files
    /// </summary>
    public class OCPIProviderLoader
    {
        private readonly ILogger _logger;
        private OCPIProvidersConfiguration _configuration;

        public OCPIProviderLoader(ILogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Loads provider configurations from a JSON file
        /// </summary>
        /// <param name="configFilePath">Path to the JSON configuration file</param>
        /// <returns>True if configuration was loaded successfully</returns>
        public bool LoadFromFile(string configFilePath)
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    Log($"OCPI provider configuration file not found: {configFilePath}");
                    return false;
                }

                var json = File.ReadAllText(configFilePath);
                return LoadFromJson(json);
            }
            catch (Exception ex)
            {
                Log($"Error loading OCPI provider configuration from file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads provider configurations from a JSON string
        /// </summary>
        /// <param name="json">JSON configuration string</param>
        /// <returns>True if configuration was loaded successfully</returns>
        public bool LoadFromJson(string json)
        {
            try
            {
                _configuration = JsonConvert.DeserializeObject<OCPIProvidersConfiguration>(json);

                if (_configuration?.Providers == null)
                {
                    Log("OCPI provider configuration is empty or invalid");
                    return false;
                }

                Log($"Loaded {_configuration.Providers.Count} OCPI provider configuration(s)");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error parsing OCPI provider configuration JSON: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates import provider instances from the loaded configuration
        /// </summary>
        /// <param name="enabledOnly">If true, only returns providers that are enabled</param>
        /// <returns>List of configured OCPI import providers</returns>
        public List<IImportProvider> CreateProviders(bool enabledOnly = true)
        {
            var providers = new List<IImportProvider>();

            if (_configuration?.Providers == null)
            {
                Log("No OCPI provider configurations loaded");
                return providers;
            }

            foreach (var config in _configuration.Providers)
            {
                if (enabledOnly && !config.IsEnabled)
                {
                    Log($"Skipping disabled provider: {config.ProviderName}");
                    continue;
                }

                if (!ValidateConfiguration(config))
                {
                    Log($"Skipping invalid provider configuration: {config.ProviderName}");
                    continue;
                }

                try
                {
                    var provider = new ImportProvider_OCPIConfigurable(config);
                    providers.Add(provider);
                    Log($"Created OCPI provider: {config.ProviderName} (DataProvider: {config.DataProviderId})");
                }
                catch (Exception ex)
                {
                    Log($"Error creating provider {config.ProviderName}: {ex.Message}");
                }
            }

            return providers;
        }

        /// <summary>
        /// Creates a single import provider by name
        /// </summary>
        /// <param name="providerName">The provider name to create</param>
        /// <returns>The import provider, or null if not found</returns>
        public IImportProvider CreateProvider(string providerName)
        {
            var config = _configuration?.Providers?
                .FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

            if (config == null)
            {
                Log($"Provider configuration not found: {providerName}");
                return null;
            }

            if (!ValidateConfiguration(config))
            {
                Log($"Invalid provider configuration: {providerName}");
                return null;
            }

            return new ImportProvider_OCPIConfigurable(config);
        }

        /// <summary>
        /// Gets the raw configuration object
        /// </summary>
        public OCPIProvidersConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the list of provider names from configuration
        /// </summary>
        public IEnumerable<string> GetProviderNames(bool enabledOnly = true)
        {
            if (_configuration?.Providers == null)
                return Enumerable.Empty<string>();

            var providers = _configuration.Providers.AsEnumerable();
            
            if (enabledOnly)
            {
                providers = providers.Where(p => p.IsEnabled);
            }

            return providers.Select(p => p.ProviderName);
        }

        /// <summary>
        /// Validates a provider configuration
        /// </summary>
        private bool ValidateConfiguration(OCPIProviderConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.ProviderName))
            {
                Log("Provider configuration missing ProviderName");
                return false;
            }

            if (config.DataProviderId <= 0)
            {
                Log($"Provider {config.ProviderName} has invalid DataProviderId");
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.LocationsEndpointUrl))
            {
                Log($"Provider {config.ProviderName} missing LocationsEndpointUrl");
                return false;
            }

            if (!Uri.TryCreate(config.LocationsEndpointUrl, UriKind.Absolute, out _))
            {
                Log($"Provider {config.ProviderName} has invalid LocationsEndpointUrl");
                return false;
            }

            return true;
        }

        private void Log(string message)
        {
            if (_logger != null)
            {
                _logger.LogInformation(message);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[OCPIProviderLoader] {message}");
            }
        }

        /// <summary>
        /// Generates example configuration JSON
        /// </summary>
        public static string GenerateExampleConfiguration()
        {
            var example = new OCPIProvidersConfiguration
            {
                Providers = new List<OCPIProviderConfiguration>
                {
                    new OCPIProviderConfiguration
                    {
                        ProviderName = "example-provider",
                        OutputNamePrefix = "example",
                        Description = "Example OCPI provider configuration",
                        DataProviderId = 99,
                        LocationsEndpointUrl = "https://api.example.com/ocpi/2.2/locations",
                        AuthHeaderKey = "Authorization",
                        CredentialKey = "OCPI-EXAMPLE",
                        DefaultOperatorId = 1234,
                        IsAutoRefreshed = true,
                        IsProductionReady = false,
                        IsEnabled = true,
                        AllowDuplicatePOIWithDifferentOperator = true,
                        OperatorMappings = new Dictionary<string, int>
                        {
                            { "Example Operator", 1234 },
                            { "Another Operator", 5678 }
                        },
                        ExcludedLocationIds = new List<string>
                        {
                            "LOC001",
                            "LOC002"
                        }
                    }
                }
            };

            return JsonConvert.SerializeObject(example, Formatting.Indented);
        }
    }
}
