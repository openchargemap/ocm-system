using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Voltrelli : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Voltrelli() : base()
        {
            ProviderName = "voltrelli";
            OutputNamePrefix = "voltrelli";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-VOLTRELLI";

            DefaultOperatorID = 3843;

            Init(dataProviderId: 37, "https://api.evozone.app/api/ocpi/v1/charging-station/list");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "Voltrelli - Evconnect",3843 }
            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var outputList = base.Process(coreRefData);

            return outputList;
        }
    }
}