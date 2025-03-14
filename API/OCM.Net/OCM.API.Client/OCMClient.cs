using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;

namespace OCM.API.Client
{
    public class APICredentials
    {
        public string Identifier { get; set; }
        public string SessionToken { get; set; }
    }

    public class SearchFilters
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Distance { get; set; }
        public DistanceUnit DistanceUnit { get; set; }

        public bool IncludeUserComments { get; set; }
        public int MaxResults { get; set; }
        public bool EnableCaching { get; set; }

        public int[] DataProviderIDs { get; set; }
        public int[] SubmissionStatusTypeIDs { get; set; }
        public int[] CountryIDs { get; set; }

        /// <summary>
        /// e.g. created_asc, modified_asc, id_asc
        /// </summary>
        public string SortBy { get; set; }

        public DateTime? ModifiedSince { get; set; }
        public DateTime? CreatedSince { get; set; }

        /// <summary>
        /// Results with ID greater than value provided, used for batch imports
        /// </summary>
        public int? GreaterThanId { get; set; }

        public bool Verbose { get; set; }

        public SearchFilters()
        {
            DistanceUnit = Common.Model.DistanceUnit.Miles;
            MaxResults = 100;
            EnableCaching = true;
            Verbose = false;
        }
    }

    public class OCMClient : IDisposable
    {
        public bool IsSandboxMode { get; set; }
        public string ServiceBaseURL { get; set; }
        public string APIKey { get; set; }

        private static HttpClient _client = new HttpClient();
        private ILogger _logger = null;

        private string _userAgent = null;
        private bool disposedValue;

        static OCMClient()
        {
            // init static properties
            _client.Timeout = TimeSpan.FromMinutes(20);
            _client.DefaultRequestHeaders.Add("User-Agent", "OCM-API-Client");
        }

        public OCMClient(string baseUrl, string apiKey, ILogger logger = null, string userAgent = "OCM-API-Client")
        {
            ServiceBaseURL = baseUrl;

            APIKey = apiKey;

            _userAgent = userAgent;

            _logger = logger;
        }


#if DEBUG

        private async Task<bool> RunAPITest(string serviceBaseURL, int testIterations)
        {
            this.ServiceBaseURL = serviceBaseURL;

            int lat = 52;
            int lng = 1;

            SearchFilters filters = new SearchFilters { Distance = 100, MaxResults = 1000, Latitude = lat, Longitude = lng, EnableCaching = false, IncludeUserComments = false };
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            for (int i = 1; i < testIterations; i++)
            {
                System.Diagnostics.Debug.WriteLine(i);
                //TODO; pass same sample lat/lng list to tests
                var r = new Random(DateTime.Now.Second);
                filters.Latitude = r.Next(50, 55);
                filters.Longitude = r.Next(-1, 1);

                var list = await this.GetPOIListAsync(filters);
                System.Diagnostics.Debug.WriteLine(i + ": POIs: " + list.Count());
            }

            stopwatch.Stop();

            System.Diagnostics.Debug.WriteLine("total ms " + this.ServiceBaseURL + " : " + stopwatch.Elapsed.TotalMilliseconds + " for " + testIterations + " iterations. avg " + (stopwatch.Elapsed.TotalMilliseconds / testIterations) + "ms per request");
            return true;
        }

        public void APITestTiming()
        {
            OCMClient client = new OCMClient("http://localhost:3000/v2", null);

            Task<bool> test1 = client.RunAPITest("http://localhost:3000/v2", 10000);
            test1.Wait();
            // Task<bool> test2 = client.RunAPITest("http://localhost:8080/v2",1000);
            //test2.Wait();
        }

#endif

        /// <summary>
        /// Get core reference data such as lookup lists
        /// </summary>
        /// <returns>  </returns>
        public async Task<CoreReferenceData> GetCoreReferenceDataAsync()
        {
            //get core reference data
            string url = ServiceBaseURL + "/referencedata/?client=ocm.api.client&output=json&enablecaching=false";
            try
            {
                string data = await FetchDataStringFromURLAsync(url);
                return JsonConvert.DeserializeObject<CoreReferenceData>(data);
            }
            catch (Exception)
            {
                //failed!
                return null;
            }
        }

        /// <summary>
        /// get list of matching POIs via API
        /// </summary>
        /// <param name="cp">  </param>
        /// <returns>  </returns>
        public async Task<IEnumerable<ChargePoint>> GetPOIListAsync(SearchFilters filters)
        {
            string url = ServiceBaseURL + "/poi/?output=json";

            if (filters.Latitude != null && filters.Longitude != null)
            {
                url += "&latitude=" + filters.Latitude + "&longitude=" + filters.Longitude;
            }

            if (filters.Distance != null)
            {
                url += "&distance=" + filters.Distance + "&distanceunit=" + filters.DistanceUnit.ToString();
            }

            if (filters.EnableCaching == false)
            {
                url += "&enablecaching=false";
            }

            if (filters.IncludeUserComments == true)
            {
                url += "&includecomments=true";
            }

            if (filters.SubmissionStatusTypeIDs != null && filters.SubmissionStatusTypeIDs.Any())
            {
                url += "&submissionstatustypeid=" + string.Join(",", filters.SubmissionStatusTypeIDs.Select(i => i.ToString()));
            }

            if (filters.DataProviderIDs != null && filters.DataProviderIDs.Any())
            {
                url += "&dataproviderid=" + string.Join(",", filters.DataProviderIDs.Select(i => i.ToString()));
            }

            if (filters.CountryIDs != null && filters.CountryIDs.Any())
            {
                url += "&countryid=" + string.Join(",", filters.CountryIDs.Select(i => i.ToString()));
            }

            if (filters.ModifiedSince.HasValue)
            {
                url += "&modifiedsince=" + filters.ModifiedSince.Value.ToString("s", CultureInfo.InvariantCulture);
            }

            if (filters.CreatedSince.HasValue)
            {
                url += "&createdsince=" + filters.CreatedSince.Value.ToString("s", CultureInfo.InvariantCulture);
            }

            if (filters.GreaterThanId.HasValue)
            {
                url += "&greaterthanid=" + filters.GreaterThanId;
            }

            if (!string.IsNullOrEmpty(filters.SortBy))
            {
                url += "&sortby=" + filters.SortBy;
            }

            url += "&verbose=" + filters.Verbose;

            url += "&maxresults=" + filters.MaxResults;

            var stopwatch = Stopwatch.StartNew();
            try
            {

                _logger?.LogInformation($"Client: Fetching data from {url}");
                string data = await FetchDataStringFromURLAsync(url);
                _logger?.LogInformation($"Client: fetch completed: {stopwatch.Elapsed.TotalSeconds}s");

                return JsonConvert.DeserializeObject<List<ChargePoint>>(data);
            }
            catch (Exception exp)
            {
                //failed!
                _logger?.LogInformation($"Client: fetch failed: {stopwatch.Elapsed.TotalSeconds}s {exp.ToString()}");
                return null;
            }
            stopwatch.Stop();
        }

        public async Task<SystemInfoResult> GetSystemStatusAsync()
        {
            string url = ServiceBaseURL + "/system/status";
            var result = await FetchDataStringFromURLAsync(url);

            return JsonConvert.DeserializeObject<SystemInfoResult>(result);
        }

        public async Task<object> Get(string endpoint)
        {
            string url = ServiceBaseURL + endpoint;
            return await FetchDataStringFromURLAsync(url);
        }
#if !PORTABLE

        public List<ChargePoint> FindSimilar(ChargePoint cp, int MinimumPercentageSimilarity)
        {
            List<ChargePoint> results = new List<ChargePoint>();
            try
            {
                string url = ServiceBaseURL + "?output=json&action=getsimilarchargepoints";

                string json = JsonConvert.SerializeObject(cp);
                string resultJSON = PostDataToURL(url, json);
                JObject o = JObject.Parse("{\"root\": " + resultJSON + "}");
                JsonSerializer serializer = new JsonSerializer();
                List<ChargePoint> c = (List<ChargePoint>)serializer.Deserialize(new JTokenReader(o["root"]), typeof(List<ChargePoint>));
                return c;
            }
            catch (Exception)
            {
                //failed!
                return null;
            }
        }

        public bool UpdatePOI(ChargePoint cp, APICredentials credentials)
        {
            //TODO: implement batch update based on item list?
            try
            {
                string url = ServiceBaseURL + "?action=cp_submission&format=json";
                url += "&Identifier=" + credentials.Identifier;
                url += "&SessionToken=" + credentials.SessionToken;

                string json = JsonConvert.SerializeObject(cp);
                string result = PostDataToURL(url, json);
                return true;
            }
            catch (Exception)
            {
                //update failed
                System.Diagnostics.Debug.WriteLine("Update Item Failed: {" + cp.ID + ": " + cp.AddressInfo.Title + "}");
                return false;
            }
        }

        public bool UpdateItems(List<ChargePoint> list)
        {
            //TODO: implement batch update based on item list?
            try
            {
                string url = ServiceBaseURL + "?action=cp_batch_submission&format=json";

                string json = JsonConvert.SerializeObject(list);
                string result = PostDataToURL(url, json);
                return true;
            }
            catch (Exception exp)
            {
                //update failed
                System.Diagnostics.Debug.WriteLine("Update Items Failed: {" + exp.Message + "}");
                return false;
            }
        }

        public async Task<Common.Model.Extended.GeocodingResult> Geocode(double latitude, double longitude)
        {
            string url = $"{ServiceBaseURL}/geocode?output=json&latitude={latitude}&longitude={longitude}";
            var result = await FetchDataStringFromURLAsync(url);
            return JsonConvert.DeserializeObject<Common.Model.Extended.GeocodingResult>(result);
        }
#endif

        public APICredentials GetCredentials(string APIKey)
        {
            //TODO: implement in API, when API Key provided, return current credentials
            return new APICredentials() { };
        }

        private async Task<string> FetchDataStringFromURLAsync(string url)
        {
            if (!string.IsNullOrEmpty(APIKey))
            {
                if (!_client.DefaultRequestHeaders.Contains("X-API-Key"))
                {
                    _client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", APIKey);
                }
            }

            HttpResponseMessage response = await _client.GetAsync(url);

            return await response.Content.ReadAsStringAsync();
        }

#if !PORTABLE

        private string PostDataToURL(string url, string data)
        {
            //http://msdn.microsoft.com/en-us/library/debx8sh9.aspx

            // Create a request using a URL that can receive a post.
            WebRequest request = WebRequest.Create(url);

            if (!string.IsNullOrEmpty(APIKey))
            {
                request.Headers.Add("X-API-Key", APIKey);
            }

            if (!string.IsNullOrEmpty(_userAgent))
            {
                request.Headers.Add("User-Agent", _userAgent);
            }

            // Set the Method property of the request to POST.
            request.Method = "POST";


            // convert data to a byte array.
            byte[] byteArray = Encoding.UTF8.GetBytes(data);

            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";

            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;

            // Get the request stream.
            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            // Get the response.
            using (WebResponse response = request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        string statusDescription = ((HttpWebResponse)response).StatusDescription;
                        //TODO: return/handle error codes
                        //Get the stream containing content returned by the server.
                        string responseFromServer = reader.ReadToEnd();
                        return responseFromServer;
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~OCMClient()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

#endif
    }
}