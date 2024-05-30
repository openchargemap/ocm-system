using Newtonsoft.Json;
using OCM.API.Common.Model;
using OCM.API.Common.Model.OCPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_OCPI : BaseImportProvider, IImportProvider
    {
        private string _authHeaderValue = "";

        private int _dataProviderId = 1;

        public Dictionary<string, int> OperatorMappings = new Dictionary<string, int>();
        internal OCPIDataAdapter _adapter;
        public ImportProvider_OCPI()
        {
            ProviderName = "ocpi";
            OutputNamePrefix = "ocpi";
            SourceEncoding = Encoding.GetEncoding("UTF-8");
            IsAutoRefreshed = true;
            AllowDuplicatePOIWithDifferentOperator = true;
        }

        /// <summary>
        /// If applicable, the key for this imports Authorization header value in our secrets vault
        /// </summary>
        public string CredentialKey { get; set; }


        /// <summary>
        /// If operator not specified in OCPI, default operator to use.
        /// </summary>
        public int? DefaultOperatorID { get; set; }
        /// <summary>
        /// Optional value for the Authorization header if required.
        /// </summary>
        public string AuthHeaderValue { set { _authHeaderValue = value; } }

        public void Init(int dataProviderId, string locationsEndpoint, string authHeaderValue = null)
        {
            AutoRefreshURL = locationsEndpoint;

            _authHeaderValue = authHeaderValue;

            _dataProviderId = dataProviderId;

            DataProviderID = _dataProviderId;
        }

        public virtual Dictionary<string, int> GetOperatorMappings()
        {
            return OperatorMappings;
        }

        private Dictionary<string, int> _unmappedOperators;
        public Dictionary<string, int> GetPostProcessingUnmappedOperators()
        {
            return _unmappedOperators;
        }

        public List<ChargePoint> Process(CoreReferenceData coreRefData)
        {

            _adapter = new OCPIDataAdapter(coreRefData, useLiveStatus: false);

            List<Model.OCPI.Location> response;
            var deserializeSettings = new JsonSerializerSettings
            {
                Error = (obj, args) =>
                {
                    var contextErrors = args.ErrorContext;
                    contextErrors.Handled = true;

                    Log($"Error parsing item {contextErrors.Error}");
                }
            };

            if (InputData.IndexOf("\"data\":") > 0)
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Model.OCPI.OcpiResponseLocationList>(InputData, deserializeSettings);
                response = result.Data.ToList();
            }
            else
            {
                response = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Model.OCPI.Location>>(InputData, deserializeSettings);
            }

            OperatorMappings = GetOperatorMappings();

            var poiResults = _adapter.FromOCPI(response, _dataProviderId, operatorMappings: OperatorMappings, defaultOperatorId: DefaultOperatorID);

            _unmappedOperators = _adapter.GetUnmappedOperators();

            return poiResults.ToList();
        }

        public new async Task<bool> LoadInputFromURL(string url)
        {
            try
            {
                if (!string.IsNullOrEmpty(_authHeaderValue))
                {
                    webClient.Headers.Add("Authorization", _authHeaderValue);
                }

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
