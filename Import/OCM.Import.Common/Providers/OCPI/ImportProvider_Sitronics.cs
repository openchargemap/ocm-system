using System.Collections.Generic;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Sitronics : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Sitronics() : base()
        {
            ProviderName = "sitronics";
            OutputNamePrefix = "sitronics";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-SITRONICS";

            DefaultOperatorID = 3656;

            Init(dataProviderId: 31, "https://ezs.sitronics.com/ocpi/2.2.1/cpo/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "SIT",3656 },
            };
        }
    }
}