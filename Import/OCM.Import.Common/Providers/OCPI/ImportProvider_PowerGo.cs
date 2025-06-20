using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_PowerGo : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_PowerGo() : base()
        {
            ProviderName = "powergo";
            OutputNamePrefix = "powergo";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-POWERGO";

            DefaultOperatorID = 3632;

            Init(dataProviderId: 38, "https://powerops.powerfield.nl/ocpi/cpo/2.2/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "PFG",3632 }
            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var outputList = base.Process(coreRefData);

            return outputList;
        }
    }

}
