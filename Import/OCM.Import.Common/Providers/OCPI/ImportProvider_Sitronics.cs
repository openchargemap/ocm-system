using Amazon.Runtime.Internal.Transform;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Sitronics : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Sitronics() : base()
        {
            ProviderName = "sitronics";
            OutputNamePrefix = "sitronics";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            Init(dataProviderId: 31, "http://ezs.sitronics.com:7059/ocpi/2.2.1/cpo/locations", "Authorization");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "SIT",3656 },
            };
        }

        List<ChargePoint> IImportProvider.Process(CoreReferenceData coreRefData)
        {
            var outputList = Process(coreRefData);
            return outputList;
        }
    }
}