using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OCM.API.Common.Model;
using OCM.API.Common.Model.OCPI;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_OCPI : BaseImportProvider, IImportProvider
    {
        private string _authHeaderKey = "Authorization";
        private string _authHeaderValue = "";
        private string _authHeaderValuePrefix = "Token ";

        private int _dataProviderId = 1;

        public Dictionary<string, int> OperatorMappings = new Dictionary<string, int>();
        public HashSet<string> ExcludedLocations = new HashSet<string>();
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
        /// When using the default Authorization header key, values without a recognized
        /// prefix (Token, Bearer, Basic) will automatically have "Token " prepended.
        /// </summary>
        public string AuthHeaderValue
        {
            get { return _authHeaderValue; }
            set
            {
                if (!string.IsNullOrEmpty(value) && string.Equals(_authHeaderKey, "Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    // If the value doesn't already have a recognized auth prefix, prepend "Token "
                    if (!value.StartsWith("Token ", StringComparison.OrdinalIgnoreCase)
                        && !value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        && !value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                    {
                        _authHeaderValue = (_authHeaderValuePrefix ?? string.Empty) + value;
                        return;
                    }
                }

                _authHeaderValue = value;
            }
        }

        public string AuthHeaderKey { set { _authHeaderKey = value; } }

        public string AuthHeaderValuePrefix
        {
            get { return _authHeaderValuePrefix; }
            set { _authHeaderValuePrefix = value; }
        }

        public void Init(int dataProviderId, string locationsEndpoint, string authHeaderKey = null)
        {
            AutoRefreshURL = locationsEndpoint;

            if (authHeaderKey != null)
            {
                _authHeaderKey = authHeaderKey;
            }

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

            var poiResults = _adapter.FromOCPI(response, _dataProviderId, operatorMappings: OperatorMappings, defaultOperatorId: DefaultOperatorID, excludedLocations: ExcludedLocations);

            _unmappedOperators = _adapter.GetUnmappedOperators();

            return poiResults.ToList();
        }

        public new async Task<bool> LoadInputFromURL(string url)
        {
            try
            {
                var handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.All
                };

                using (var httpClient = new HttpClient(handler))
                {
                    if (!string.IsNullOrEmpty(_authHeaderValue))
                    {
                        httpClient.DefaultRequestHeaders.Remove(_authHeaderKey);
                        httpClient.DefaultRequestHeaders.Add(_authHeaderKey, _authHeaderValue);

                        Log($"Auth Header Used {_authHeaderKey}:{_authHeaderValue}");
                    }
                    httpClient.DefaultRequestHeaders.Remove("User-Agent");
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "openchargemap-OCPI-import/1.0");
                    httpClient.DefaultRequestHeaders.Remove("Accept");
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/json; charset=utf-8");

                    int offset = 0;
                    int limit = 100; // Default page size, can be adjusted
                    bool supportsPaging = false;
                    int? totalCount = null;
                    var allItems = new List<object>();
                    string baseUrl = url;
                    bool wasWrappedObject = false;
                    bool firstResponse = true;

                    // Check if the URL is an OCPI versions endpoint and resolve to locations if so
                    baseUrl = await ResolveVersionsEndpointIfNeeded(httpClient, baseUrl);

                    do
                    {
                        string pagedUrl = baseUrl;
                        if (supportsPaging || offset > 0)
                        {
                            var uriBuilder = new UriBuilder(baseUrl);
                            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                            query.Set("offset", offset.ToString());
                            query.Set("limit", limit.ToString());
                            uriBuilder.Query = query.ToString();
                            pagedUrl = uriBuilder.ToString();
                        }

                        var response = await httpClient.GetAsync(pagedUrl);

                        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            var maskedValue = !string.IsNullOrEmpty(_authHeaderValue) && _authHeaderValue.Length > 10
                                ? _authHeaderValue.Substring(0, 10) + "..."
                                : _authHeaderValue ?? "(none)";

                            Log($"Authorization failed ({(int)response.StatusCode} {response.StatusCode}). Header used: {_authHeaderKey}: {maskedValue}");
                        }

                        response.EnsureSuccessStatusCode();
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (!supportsPaging && response.Headers.Contains("X-Total-Count"))
                        {
                            supportsPaging = true;
                            var headerValue = response.Headers.GetValues("X-Total-Count").FirstOrDefault();
                            if (int.TryParse(headerValue, out int parsedTotal))
                            {
                                totalCount = parsedTotal;
                            }
                        }

                        // Deserialize and aggregate results
                        if (responseContent.TrimStart().StartsWith("{"))
                        {
                            if (firstResponse) wasWrappedObject = true;
                            // OCPI response with data property
                            var obj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(responseContent);
                            var dataToken = obj["data"];
                            if (dataToken != null && dataToken.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                            {
                                foreach (var item in dataToken)
                                {
                                    allItems.Add(item);
                                }
                            }
                        }
                        else if (responseContent.TrimStart().StartsWith("["))
                        {
                            if (firstResponse) wasWrappedObject = false;
                            // Array response
                            var arr = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(responseContent);
                            foreach (var item in arr)
                            {
                                allItems.Add(item);
                            }
                        }
                        else
                        {
                            // Unexpected format, just assign
                            InputData = responseContent;
                            return true;
                        }

                        offset += limit;
                        firstResponse = false;
                    } while (supportsPaging && totalCount.HasValue && offset < totalCount.Value);

                    // Compose aggregated result
                    if (allItems.Count > 0)
                    {
                        // If original response was wrapped in { data: [...] }, wrap result similarly
                        if (wasWrappedObject)
                        {
                            var resultObj = new Newtonsoft.Json.Linq.JObject();
                            resultObj["data"] = new Newtonsoft.Json.Linq.JArray(allItems);
                            InputData = resultObj.ToString();
                        }
                        else
                        {
                            InputData = JsonConvert.SerializeObject(allItems);
                        }
                    }
                    else
                    {
                        InputData = "";
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($": Failed to fetch input from url :{url} Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Preferred OCPI versions in priority order (highest first)
        /// </summary>
        private static readonly string[] PreferredVersions = ["2.2.1", "2.2", "2.1.1"];

        /// <summary>
        /// If the given URL is an OCPI versions endpoint, resolves and returns the locations endpoint URL.
        /// Otherwise returns the original URL unchanged.
        /// </summary>
        private async Task<string> ResolveVersionsEndpointIfNeeded(HttpClient httpClient, string url)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return url; // let the main fetch handle the error
                }

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content) || !content.TrimStart().StartsWith("{"))
                {
                    return url;
                }

                var obj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(content);
                var dataToken = obj?["data"];
                if (dataToken == null || dataToken.Type != Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    return url;
                }

                // Check if this looks like a versions list (items have "version" and "url" but not location properties)
                var firstItem = dataToken.First;
                if (firstItem == null || firstItem["version"] == null || firstItem["url"] == null
                    || firstItem["evses"] != null || firstItem["coordinates"] != null)
                {
                    return url;
                }

                // It's a versions endpoint - pick the highest supported version
                var versions = dataToken
                    .Select(v => new { Version = (string)v["version"], Url = (string)v["url"] })
                    .Where(v => !string.IsNullOrEmpty(v.Version) && !string.IsNullOrEmpty(v.Url))
                    .ToList();

                if (!versions.Any())
                {
                    return url;
                }

                var selectedVersion = versions.FirstOrDefault(v => PreferredVersions.Contains(v.Version))
                    ?? versions.OrderByDescending(v => v.Version).First();

                Log($"Detected OCPI versions endpoint. Resolving version {selectedVersion.Version} from {selectedVersion.Url}");

                // Fetch the version detail to find the locations endpoint
                var versionDetailResponse = await httpClient.GetAsync(selectedVersion.Url);
                if (!versionDetailResponse.IsSuccessStatusCode)
                {
                    Log($"Failed to fetch version detail: HTTP {(int)versionDetailResponse.StatusCode}");
                    return url;
                }

                var versionDetailContent = await versionDetailResponse.Content.ReadAsStringAsync();
                var versionDetailObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(versionDetailContent);
                var endpoints = versionDetailObj?["data"]?["endpoints"];

                if (endpoints == null || endpoints.Type != Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    Log("Version detail does not contain an 'endpoints' array");
                    return url;
                }

                var locationsEndpoint = endpoints.FirstOrDefault(e =>
                    string.Equals((string)e["identifier"], "locations", StringComparison.OrdinalIgnoreCase));

                if (locationsEndpoint == null)
                {
                    Log("No 'locations' endpoint found in version detail. Available: "
                        + string.Join(", ", endpoints.Select(e => (string)e["identifier"])));
                    return url;
                }

                var locationsUrl = (string)locationsEndpoint["url"];
                if (string.IsNullOrEmpty(locationsUrl))
                {
                    Log("The 'locations' endpoint has an empty URL");
                    return url;
                }

                Log($"Resolved locations endpoint: {locationsUrl}");
                return locationsUrl;
            }
            catch (Exception ex)
            {
                Log($"Failed to check for versions endpoint: {ex.Message}");
                return url; // fall through to normal fetch
            }
        }
    }
}
