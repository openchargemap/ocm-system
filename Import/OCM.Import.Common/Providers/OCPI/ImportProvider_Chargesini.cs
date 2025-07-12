using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Chargesini : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Chargesini() : base()
        {
            ProviderName = "chargesini.com";
            OutputNamePrefix = "chargesini.com";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-CHARGESINI";

            DefaultOperatorID = 3660; // ChargeSini
            Init(dataProviderId: 41, "https://web-api.chargesini.com/ocpi/cpo/2.2/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "Chargesini",3660 }
            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var outputList = base.Process(coreRefData);

            foreach (var poi in outputList)
            {
                // chargesini is unusual in that it publishes private locations (not recommend by OCPI) with an indicator in the title, so post-process those here, leave "restricted" in the title for clarity
                if (poi.AddressInfo.Title.StartsWith("[public]", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    poi.UsageTypeID = (int)StandardUsageTypes.Public_MembershipRequired;
                    poi.AddressInfo.Title = poi.AddressInfo.Title.Replace("[Public] ", "");
                }
                else if (poi.AddressInfo.Title.StartsWith("[restricted]", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    poi.UsageTypeID = (int)StandardUsageTypes.PrivateRestricted;
                }
            }
            return outputList;
        }
    }

}
