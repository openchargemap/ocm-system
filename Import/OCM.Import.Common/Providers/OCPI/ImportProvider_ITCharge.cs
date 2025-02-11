using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_ITCharge : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_ITCharge() : base()
        {
            ProviderName = "itcharge.ru";
            OutputNamePrefix = "itcharge.ru";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-ITCHARGE-CRED";

            DefaultOperatorID = 3650;

            Init(dataProviderId: 36, "https://ocpi.itcharge.ru/cpo/2.2.1/locations/");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {

            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var outputList = base.Process(coreRefData);

            return outputList;
        }
    }
}