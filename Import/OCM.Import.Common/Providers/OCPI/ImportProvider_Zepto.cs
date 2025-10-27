using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Zepto : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Zepto() : base()
        {
            ProviderName = "zepto.pl";
            OutputNamePrefix = "zepto.pl";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            DefaultOperatorID = 3475; // ZEPTO

            Init(dataProviderId: 44, "https://zepto.pl/ocpi/cpo/2.2/");
        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            return new Dictionary<string, int>()
            {
                { "ZEPTO", 3475 }
            };
        }

        public new List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var outputList = base.Process(coreRefData);

            return outputList;
        }
    }
}
