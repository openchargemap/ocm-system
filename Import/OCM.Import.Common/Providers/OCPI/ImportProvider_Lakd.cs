using Amazon.Runtime.Internal.Transform;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        List<ChargePoint> IImportProvider.Process(CoreReferenceData coreRefData)
        {
            InputData = InputData.Replace("\"location\"", "\"coordinates\"");

            var outputList = Process(coreRefData);

            return outputList;
        }
    }
}