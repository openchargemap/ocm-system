using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_EV24 : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_EV24() : base()
        {
            ProviderName = "ev24.cloud";
            OutputNamePrefix = "ev24.cloud";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-EV24";

            DefaultOperatorID = 3898;

            Init(dataProviderId: 43, "https://api.ev24.cloud/ocpi/2.2.1/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "EV24", 3898 }
            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var outputList = base.Process(coreRefData);

            // update title to include street name for reach item, otherwise deduplicate will discard on duplicate title
            foreach (var cp in outputList)
            {
                if (!string.IsNullOrEmpty(cp.AddressInfo?.Title) && !string.IsNullOrEmpty(cp.AddressInfo?.AddressLine1))
                {
                    cp.AddressInfo.Title = $"{cp.AddressInfo.Title}, {cp.AddressInfo.AddressLine1}";
                }
            }
            return outputList;
        }
    }
}
