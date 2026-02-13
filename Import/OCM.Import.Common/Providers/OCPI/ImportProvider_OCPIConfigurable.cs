using System.Collections.Generic;

namespace OCM.Import.Providers.OCPI
{
    /// <summary>
    /// A configurable OCPI import provider that can be instantiated from configuration
    /// rather than requiring a custom class for each provider.
    /// </summary>
    public class ImportProvider_OCPIConfigurable : ImportProvider_OCPI, IImportProvider
    {
        private readonly OCPIProviderConfiguration _config;

        /// <summary>
        /// Creates a new configurable OCPI import provider from the given configuration
        /// </summary>
        /// <param name="config">The provider configuration</param>
        public ImportProvider_OCPIConfigurable(OCPIProviderConfiguration config) : base()
        {
            _config = config;

            // Set provider name and output prefix
            ProviderName = config.ProviderName;
            OutputNamePrefix = !string.IsNullOrEmpty(config.OutputNamePrefix) 
                ? config.OutputNamePrefix 
                : config.ProviderName;

            // Set flags
            IsAutoRefreshed = config.IsAutoRefreshed;
            IsProductionReady = config.IsProductionReady;
            AllowDuplicatePOIWithDifferentOperator = config.AllowDuplicatePOIWithDifferentOperator;

            // Set credential key if specified
            CredentialKey = config.CredentialKey;

            // Set default operator
            DefaultOperatorID = config.DefaultOperatorId;

            // Copy operator mappings
            if (config.OperatorMappings != null)
            {
                OperatorMappings = new Dictionary<string, int>(config.OperatorMappings);
            }

            // Copy excluded locations
            if (config.ExcludedLocationIds != null)
            {
                foreach (var locationId in config.ExcludedLocationIds)
                {
                    ExcludedLocations.Add(locationId);
                }
            }

            // Initialize the provider with endpoint URL
            Init(
                dataProviderId: config.DataProviderId,
                locationsEndpoint: config.LocationsEndpointUrl,
                authHeaderKey: config.AuthHeaderKey
            );
        }

        /// <summary>
        /// Returns the operator mappings from configuration
        /// </summary>
        public override Dictionary<string, int> GetOperatorMappings()
        {
            return OperatorMappings ?? new Dictionary<string, int>();
        }

        /// <summary>
        /// Gets the provider configuration
        /// </summary>
        public OCPIProviderConfiguration Configuration => _config;
    }
}
