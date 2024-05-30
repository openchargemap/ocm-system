using System.Collections.Generic;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_ElectricEra : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_ElectricEra() : base()
        {
            ProviderName = "electricera";
            OutputNamePrefix = "electricera";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = null; // no credentials

            Init(dataProviderId: 35, "https://ocpi-http.app.electricera.tech/ocpi/2.2/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "Electric Era", 3789}
            };
        }
    }
}