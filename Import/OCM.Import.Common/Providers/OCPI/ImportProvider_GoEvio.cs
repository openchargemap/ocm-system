using OCM.API.Common.Model;
using System.Collections.Generic;

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

            CredentialKey = "OCPI-EVIO";

            DefaultOperatorID = 3792;

            Init(dataProviderId: 30, "https://api.go-evio.com/locations","apikey");
        }
    }
}