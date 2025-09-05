using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Otopriz : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Otopriz() : base()
        {
            ProviderName = "otopriz.com.tr";
            OutputNamePrefix = "otopriz.com.tr";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = null;

            DefaultOperatorID = 3807;

            Init(dataProviderId: 42, "https://otopriz.mapcontentpartners.service.electroop.io/2.2/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "OTO",3807 }
            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var outputList = base.Process(coreRefData);

            return outputList;
        }
    }

}
