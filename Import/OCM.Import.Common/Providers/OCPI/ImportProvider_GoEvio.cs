using Amazon.Runtime.Internal.Transform;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_GoEvio : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_GoEvio() : base()
        {
            ProviderName = "go-evio";
            OutputNamePrefix = "go-evio";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            Init(dataProviderId: 30, "https://api.go-evio.com/locations", "apikey");
        }

        List<ChargePoint> IImportProvider.Process(CoreReferenceData coreRefData)
        {
            var outputList = Process(coreRefData);
            return outputList;
        }
    }
}