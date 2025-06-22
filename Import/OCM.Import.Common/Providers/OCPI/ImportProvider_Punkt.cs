using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Punkt : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Punkt() : base()
        {
            ProviderName = "punkt-e";
            OutputNamePrefix = "punkt-e";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-PUNKT";

            DefaultOperatorID = 3636;

            Init(dataProviderId: 39, "https://io.api.punkt-e.io/2.2.1/cpo/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "TBM", 3634 }
            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var outputList = base.Process(coreRefData);

            return outputList;
        }
    }
}