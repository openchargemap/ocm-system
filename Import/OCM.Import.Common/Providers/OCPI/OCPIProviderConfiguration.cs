using System.Collections.Generic;

namespace OCM.Import.Providers.OCPI
{
    /// <summary>
    /// Configuration for a single OCPI import provider
    /// </summary>
    public class OCPIProviderConfiguration
    {
        /// <summary>
        /// Unique name/identifier for this provider (e.g., "electricera", "mobie.pt")
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// Prefix used for output files
        /// </summary>
        public string OutputNamePrefix { get; set; }

        /// <summary>
        /// OCM Data Provider ID that this import maps to
        /// </summary>
        public int DataProviderId { get; set; }

        /// <summary>
        /// OCPI locations endpoint URL
        /// </summary>
        public string LocationsEndpointUrl { get; set; }

        /// <summary>
        /// Optional: Custom authorization header key (defaults to "Authorization")
        /// </summary>
        public string AuthHeaderKey { get; set; }

        /// <summary>
        /// Optional: Key used to lookup credentials from the credentials vault/settings
        /// If null, no authorization header is sent
        /// </summary>
        public string CredentialKey { get; set; }

        /// <summary>
        /// Default OCM Operator ID to use when operator cannot be determined from OCPI data
        /// </summary>
        public int? DefaultOperatorId { get; set; }

        /// <summary>
        /// Whether this provider is enabled for automatic refresh
        /// </summary>
        public bool IsAutoRefreshed { get; set; } = true;

        /// <summary>
        /// Whether this provider is ready for production imports
        /// </summary>
        public bool IsProductionReady { get; set; } = true;

        /// <summary>
        /// Whether to allow duplicate POI entries with different operators at the same location
        /// </summary>
        public bool AllowDuplicatePOIWithDifferentOperator { get; set; } = true;

        /// <summary>
        /// Mapping of OCPI operator names/codes to OCM Operator IDs
        /// Key: Operator name as it appears in OCPI data
        /// Value: OCM Operator ID
        /// </summary>
        public Dictionary<string, int> OperatorMappings { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// List of location IDs to exclude from import
        /// </summary>
        public List<string> ExcludedLocationIds { get; set; } = new List<string>();

        /// <summary>
        /// Optional description of this provider configuration
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// If true, this provider configuration is enabled for import
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Root configuration containing all OCPI provider configurations
    /// </summary>
    public class OCPIProvidersConfiguration
    {
        /// <summary>
        /// List of all configured OCPI providers
        /// </summary>
        public List<OCPIProviderConfiguration> Providers { get; set; } = new List<OCPIProviderConfiguration>();
    }
}
