using OCM.API.Common.Model;
using OCM.API.Common.Model.OCPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCM.Import.Providers
{
    internal class ImportProvider_OCPI : BaseImportProvider, IImportProvider
    {
        private string _authHeaderKey = "";
        private string _authHeaderValue = "";

        private int _dataProviderId = 1;

        public ImportProvider_OCPI()
        {
            this.ProviderName = "ocpi";
            OutputNamePrefix = "ocpi";
            SourceEncoding = Encoding.GetEncoding("UTF-8");
            IsAutoRefreshed = true;
            AllowDuplicatePOIWithDifferentOperator = true;

        }

        public void Init(int dataProviderId, string locationsEndpoint, string authHeaderKey, string authHeaderValue)
        {
            AutoRefreshURL = locationsEndpoint;

            _authHeaderKey = authHeaderKey;

            _authHeaderValue = authHeaderValue;

            _dataProviderId = dataProviderId;
        }

        public List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var adapter = new OCPIDataAdapter(coreRefData, useLiveStatus: false);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OCM.Model.OCPI.Location>>(InputData);

            var poiResults = adapter.FromOCPI(response, _dataProviderId);

            return poiResults.ToList();
        }

        public new bool LoadInputFromURL(string url)
        {
            try
            {
                webClient.Headers.Add(_authHeaderKey, _authHeaderValue);
                webClient.Headers.Add("Content-Type", "application/json; charset=utf-8");

                InputData = webClient.DownloadString(url);

                return true;
            }
            catch (Exception)
            {
                Log(": Failed to fetch input from url :" + url);
                return false;
            }
        }
    }
}
