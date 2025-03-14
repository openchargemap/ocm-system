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

            ExcludedLocations = new HashSet<string> { "c007dc86-07e1-449e-add9-efa44e9e46e8", "2a46e8c0-432d-41e2-810a-b0d3a348861e", "41c38115-4bdf-4ad9-8e1c-3863e13a9cff" , "03c7019c-7e0e-4c40-8d45-3199e9d954d4" };

            Init(dataProviderId: 36, "https://ocpi.itcharge.ru/cpo/2.2.1/locations/");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "ITC",3650 }
            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {

            var outputList = base.Process(coreRefData);

            return outputList;
        }
    }
}