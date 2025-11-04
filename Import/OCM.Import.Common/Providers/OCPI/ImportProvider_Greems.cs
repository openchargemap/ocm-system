using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Greems : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Greems() : base()
        {
            ProviderName = "greems.io";
            OutputNamePrefix = "greems.io";
            CredentialKey = "OCPI-GREEMS";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            DefaultOperatorID = 3899; 

            Init(dataProviderId: 45, "https://cpo.ocpi.cpo.greems.io/ocpi/cpo/2.2.1/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "GRE", (int)DefaultOperatorID }
            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var outputList = base.Process(coreRefData);

            return outputList;
        }
    }
}
