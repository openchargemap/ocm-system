using OCM.API.Common.Model;
using System.Collections.Generic;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Lakd : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Lakd() : base()
        {
            ProviderName = "lakd.lt";
            OutputNamePrefix = "lakd.lt";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = null; // no credentials

            Init(dataProviderId: 32, "https://ev.lakd.lt/open_source/ocpi/locations");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "Ignitis UAB",3688},
                { "UAB „Stova“",3689 },
                { "In Balance grid, UAB",3690 },
                { "AB Lietuvos automobilių kelių direkcija",3691},
                { "Eldrive Lithuania, UAB",3692},
                { "IONITY GmbH",3299},
                { "Lidl Lietuva",38}
            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            //InputData = InputData.Replace("\"Not_implemented\"", "null");
            

            var outputList = base.Process(coreRefData);

            return outputList;
        }
    }
}