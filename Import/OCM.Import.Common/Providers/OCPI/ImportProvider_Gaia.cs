using System.Collections.Generic;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Gaia : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Gaia() : base()
        {
            ProviderName = "gaia";
            OutputNamePrefix = "gaia";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-GAIA";

            Init(dataProviderId: 33, "https://ocpi.longship.io/ocpi/2.2/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "Gaia Charge", 3790}
            };
        }
    }
}