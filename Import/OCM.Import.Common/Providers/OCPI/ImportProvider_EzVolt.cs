using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_EzVolt : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_EzVolt() : base()
        {
            ProviderName = "ezvolt.com.br";
            OutputNamePrefix = "ezvolt.com.br";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-EZVOLT";

            DefaultOperatorID = 3564; // EZVolt
            Init(dataProviderId: 40, "https://api3.mycharge.com.br/ocpi/2.2.1/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "EZVolt",3564 }
            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var outputList = base.Process(coreRefData);

            return outputList;
        }
    }

}
