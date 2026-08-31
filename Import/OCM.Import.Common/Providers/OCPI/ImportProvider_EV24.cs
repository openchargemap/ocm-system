using System.Collections.Generic;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_EV24 : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_EV24() : base()
        {
            ProviderName = "ev24.cloud";
            OutputNamePrefix = "ev24.cloud";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-EV24";

            DefaultOperatorID = 3898;

            // every EV24 location is named "EV24 Charging Station", so the address has to be
            // appended or deduplication discards all but the first POI on matching title
            AppendAddressToTitle = true;

            Init(dataProviderId: 43, "https://api.ev24.cloud/ocpi/2.2.1/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "EV24", 3898 }
            };
        }
    }
}
