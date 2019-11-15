
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.API.Common.Model.Extended;
using OCM.API.InputProviders;
using OCM.API.OutputProviders;

namespace OCM.API
{

    public static class HttpRequestExtensions
    {
        public static Uri GetUri(this HttpRequest request)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = request.Host.Host,
                Port = request.Host.Port.GetValueOrDefault(80),
                Path = request.Path.ToString(),
                Query = request.QueryString.ToString()
            };
            return uriBuilder.Uri;
        }

        public static string UserAgent(this HttpRequest request)
        {
            return request.Headers["User-Agent"].ToString();
        }
    }
    /// <summary>
    /// Basic HTTP handler to parse parameters and stream response via various output type providers
    /// </summary>
    public class CompatibilityAPICoreHTTPHandler : ServiceParameterParser
    {
        protected int APIBehaviourVersion { get; set; }

        protected string DefaultAction { get; set; }

        /// <summary>
        /// If true, handler expects queries using POST instead of GET
        /// </summary>
        protected bool IsQueryByPost { get; set; }

        public CompatibilityAPICoreHTTPHandler()
        {
            APIBehaviourVersion = 0;
            DefaultAction = null;
            IsQueryByPost = false;
        }

        protected void ClearResponse(HttpContext context)
        {
            //context.Response.ClearHeaders();
            //context.Response.ClearContent();
            context.Response.Clear();
        }

        public bool IsRequestByRobot(HttpContext context)
        {

            try
            {
                if (context.Request.UserAgent() != null)
                {
                    var userAgent = context.Request.UserAgent().ToLower();
                    if (userAgent.Contains("robot") || userAgent.Contains("crawler") || userAgent.Contains("spider") || userAgent.Contains("slurp") || userAgent.Contains("googlebot") || userAgent.Contains("kml-google") || userAgent.Contains("apache-httpclient") || userAgent.Equals("apache") || userAgent.StartsWith("nameofagent") || userAgent.StartsWith("php") || userAgent.Contains("okhttp/3.9.1")) // || userAgent.Contains("EVCompany") || userAgent.Contains("MOB30")

                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                //exception identifying user agent
            }
            return false;

        }

        /// <summary>
        /// Handle input to API
        /// </summary>
        /// <param name="context"></param>
        private async Task PerformInput(HttpContext context)
        {
            if (!bool.Parse(ConfigurationManager.AppSettings["EnableDataWrites"]))
            {
                await OutputBadRequestMessage(context, "API is read only. Submissions not currently being accepted.");
                return;
            }

            OCM.API.InputProviders.IInputProvider inputProvider = null;

            var filter = new APIRequestParams();

            //set defaults
            var paramList = new NullSafeDictionary<string, string>();
            foreach (var k in context.Request.Query.Keys)
            {
                paramList.Add(k.ToLower(), context.Request.Query[k]);
            }

            filter.ParseParameters(filter, paramList);

            //override ?v=2 etc if called via /api/v2/ or /api/v1
            if (APIBehaviourVersion > 0) filter.APIVersion = APIBehaviourVersion;
            if (APIBehaviourVersion >= 2) filter.Action = DefaultAction;

            if (context.Request.GetUri().Host.ToLower().StartsWith("api") && filter.APIVersion == null)
            {
                //API version is mandatory for api V2 onwards via api.openchargemap.* hostname
                await OutputBadRequestMessage(context, "mandatory API Version not specified in request");
                return;
            }

            bool performSubmissionCompletedRedirects = false;

            //Use JSON format submission if explictly specified or by default if API v3
            if (context.Request.Query["format"] == "json" || (String.IsNullOrEmpty(context.Request.Query["format"]) && filter.APIVersion >= 3))
            {
                inputProvider = new InputProviders.JSONInputProvider();
            }
            else
            {
                inputProvider = new InputProviders.HTMLFormInputProvider();
                performSubmissionCompletedRedirects = true;
            }

            SubmissionManager submissionManager = new SubmissionManager();

            //attempt to determine user from api call
            User user = inputProvider.GetUserFromAPICall(context);

            if (user != null && user.IsCurrentSessionTokenValid == false)
            {
                //session token provided didn't match latest in user details, reject user details.
                context.Response.StatusCode = 401;
            }
            else
            {
                //allow contact us input whether use is authenticated or not
                if (context.Request.Query["action"] == "contactus_submission" || filter.Action == "contact")
                {
                    ContactSubmission contactSubmission = new ContactSubmission();
                    bool processedOK = inputProvider.ProcessContactUsSubmission(context, ref contactSubmission);

                    bool resultOK = submissionManager.SendContactUsMessage(contactSubmission.Name, contactSubmission.Email, contactSubmission.Comment);
                    if (resultOK == true)
                    {
                        await context.Response.WriteAsync("OK");
                    }
                    else
                    {
                        await context.Response.WriteAsync("Error");
                    }
                }

                //if user not authenticated reject any other input
                if (user == null)
                {
                    context.Response.StatusCode = 401;
                    return;
                }

                //gather input variables
                if (context.Request.Query["action"] == "cp_submission" || filter.Action == "poi")
                {
                    //gather/process data for submission
                    var processingResult = await inputProvider.ProcessEquipmentSubmission(context);

                    ValidationResult submissionResult = new ValidationResult { IsValid = false };

                    if (processingResult.IsValid)
                    {
                        //perform submission
                        submissionResult = submissionManager.PerformPOISubmission((ChargePoint)processingResult.Item, user);
                    }

                    if (processingResult.IsValid && submissionResult.IsValid)
                    {
                        if (performSubmissionCompletedRedirects)
                        {
                            context.Response.Redirect("http://openchargemap.org/submissions/complete.aspx", true);
                        }
                        else
                        {
                            context.Response.StatusCode = 202;
                        }
                    }
                    else
                    {
                        // validation or submission failed
                        if (performSubmissionCompletedRedirects)
                        {
                            context.Response.Redirect("http://openchargemap.org/submissions/error.aspx", true);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync(processingResult.IsValid ? submissionResult.Message : processingResult.Message);
                        }
                    }
                }

                if (context.Request.Query["action"] == "cp_batch_submission")
                {
                    var sr = new System.IO.StreamReader(context.Request.Body);
                    string contentJson = sr.ReadToEnd().Trim();

                    var list = JsonConvert.DeserializeObject<List<ChargePoint>>(contentJson);
                    foreach (var cp in list)
                    {
                        var validationResult = POIManager.IsValid(cp);

                        ValidationResult submissionResult = new ValidationResult { IsValid = false };

                        if (validationResult.IsValid)
                        {
                            //perform submission
                            submissionResult = submissionManager.PerformPOISubmission(cp, user, performCacheRefresh: false);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Invalid POI: " + cp.ID);
                        }


                    }

                    // refresh cache

                    var cacheTask = System.Threading.Tasks.Task.Run(async () =>
                    {
                        return await Core.Data.CacheManager.RefreshCachedData();
                    });
                    cacheTask.Wait();

                    context.Response.StatusCode = 202;


                }

                if (context.Request.Query["action"] == "comment_submission" || filter.Action == "comment")
                {
                    UserComment comment = new UserComment();
                    bool processedOK = await inputProvider.ProcessUserCommentSubmission(context, comment);
                    if (processedOK == true)
                    {
                        //perform submission
                        int result = submissionManager.PerformSubmission(comment, user);
                        if (filter.APIVersion >= 3)
                        {
                            if (result > 0)
                            {
                                await OutputSubmissionReceivedMessage(context, "OK", true);
                            }
                            else
                            {
                                await OutputBadRequestMessage(context, "Failed");
                            }
                        }
                        else
                        {
                            if (result >= 0)
                            {
                                await context.Response.WriteAsync("OK:" + result);
                            }
                            else
                            {
                                await context.Response.WriteAsync("Error:" + result);
                            }
                        }
                    }
                    else
                    {
                        await context.Response.WriteAsync("Error: Validation Failed");
                    }
                }

                if (context.Request.Query["action"] == "mediaitem_submission" || filter.Action == "mediaitem")
                {
                    var p = inputProvider;// as OCM.API.InputProviders.HTMLFormInputProvider;
                    MediaItem m = new MediaItem();
                    bool accepted = false;
                    string msg = "";
                    try
                    {
                        var tempPath = System.IO.Path.GetTempPath() + "\\_ocm";
                        if (!System.IO.Directory.Exists(tempPath))
                        {
                            System.IO.Directory.CreateDirectory(tempPath);
                        }

                        accepted = await p.ProcessMediaItemSubmission(tempPath, context, m, user.ID);
                    }
                    catch (Exception exp)
                    {
                        msg += exp.ToString();
                    }
                    if (accepted)
                    {
                        submissionManager.PerformSubmission(m, user);
                        //OutputSubmissionReceivedMessage(context, "OK :" + m.ID, true);

                        if (filter.APIVersion >= 3)
                        {
                            await OutputSubmissionReceivedMessage(context, "OK", true);
                        }
                        else
                        {
                            await context.Response.WriteAsync("OK");
                        }
                    }
                    else
                    {
                        if (filter.APIVersion >= 3)
                        {
                            await OutputBadRequestMessage(context, "Failed");
                        }
                        else
                        {
                            await context.Response.WriteAsync("Error");
                        }
                    }
                }
            }
        }

        private async Task OutputBadRequestMessage(HttpContext context, string message, int statusCode = 400)
        {
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync("{\"status\":\"error\",\"description\":\"" + message + "\"}");
        }

        private async Task OutputSubmissionReceivedMessage(HttpContext context, string message, bool processed)
        {
            if (processed)
            {
                context.Response.StatusCode = 200;
            }
            else
            {
                context.Response.StatusCode = 202;
            }

            try
            {
                await context.Response.WriteAsync("{\"status\":\"OK\",\"description\":\"" + message + "\"}");
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Handle output from API
        /// </summary>
        /// <param name="context"></param>
        private async Task PerformOutput(HttpContext context)
        {
            //context.Sc.Server.ScriptTimeout = 120; //max script execution time is 2 minutes

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            //decide correct reponse type
            IOutputProvider outputProvider = null;
            var filter = new APIRequestParams();

            //set defaults
            string outputType = "xml";

            filter.DistanceUnit = DistanceUnit.Miles;
            filter.MaxResults = 100;
            filter.EnableCaching = true;

            var paramList = new NullSafeDictionary<string, string>();
            foreach (var k in context.Request.Query.Keys)
            {
                paramList.Add(k.ToLower(), context.Request.Query[k]);
            }
            filter.ParseParameters(filter, paramList);

            //override ?v=2 etc if called via /api/v2/ or /api/v1
            if (APIBehaviourVersion > 0) filter.APIVersion = APIBehaviourVersion;
            if (APIBehaviourVersion >= 2) filter.Action = DefaultAction;

            if (context.Request.GetUri().Host.ToLower().StartsWith("api") && filter.APIVersion == null)
            {
                //API version is mandatory for api V2 onwards via api.openchargemap.* hostname
                await OutputBadRequestMessage(context, "mandatory API Version not specified in request");
                return;
            }

            if (!String.IsNullOrEmpty(context.Request.Query["output"]))
            {
                outputType = ParseString(context.Request.Query["output"]);
            }
            else
            {
                //new default after API V2 is json instead of XML
                if (filter.APIVersion >= 2)
                {
                    outputType = "json";
                }
            }

            //change defaults and override settings for deprecated api features
            if (filter.APIVersion >= 2)
            {
                //the following output types are deprecated and will default as JSON
                if (outputType == "carwings" || outputType == "rss")
                {
                    await OutputBadRequestMessage(context, "specified output type not supported in this API version");
                    return;
                }
            }

            if (IsRequestByRobot(context))
            {
                await OutputBadRequestMessage(context, "API requests by robots are temporarily disabled.", statusCode: 503);
                return;
            }

            //determine output provider
            switch (outputType)
            {
                case "xml":
                    outputProvider = new XMLOutputProvider();
                    break;

                case "carwings":
                case "rss":
                    outputProvider = new RSSOutputProvider();
                    if (outputType == "carwings") ((RSSOutputProvider)outputProvider).EnableCarwingsMode = true;
                    break;

                case "json":
                    outputProvider = new JSONOutputProvider();
                    break;

                case "geojson":
                    outputProvider = new GeoJSONOutputProvider();
                    break;

                case "csv":
                    outputProvider = new CSVOutputProvider();
                    break;

                case "kml":
                    outputProvider = new KMLOutputProvider(KMLOutputProvider.KMLVersion.V2);
                    break;

                default:
                    outputProvider = new XMLOutputProvider();
                    break;
            }

            if (outputProvider != null)
            {
                //FIXME: context.Response.ContentEncoding = Encoding.Default;
                context.Response.ContentType = outputProvider.ContentType;

                /*if (!(filter.Action == "getcorereferencedata" && String.IsNullOrEmpty(context.Request.Query["SessionToken"])))
                {
                    //by default output is cacheable for 24 hrs, unless requested by a specific user.
                    context.Response.Cache.SetExpires(DateTime.Now.AddDays(1));
                    context.Response.Cache.SetCacheability(HttpCacheability.Public);
                    context.Response.Cache.SetValidUntilExpires(true);
                }*/

                /*if (ConfigurationManager.AppSettings["EnableOutputCompression"] == "true")
                {
                    //apply compression if accepted
                    context.Request.Headers.TryGetValue("Accept-Encoding", out string encodings);

                    if (encodings != null)
                    {
                        encodings = encodings.ToLower();

                        if (encodings.ToLower().Contains("gzip"))
                        {
                            context.Response.Filter = new GZipStream(context.Response.Filter, CompressionLevel.Optimal, false);
                            context.Response.Headers.TryAdd("Content-Encoding", "gzip");
                            //context.Trace.Warn("GZIP Compression on");
                        }
                        else if (encodings.ToLower().Contains("deflate"))
                        {
                            context.Response.Filter = new DeflateStream(context.Response.Filter, CompressionMode.Compress);
                            context.Response.Headers.TryAdd("Content-Encoding", "deflate");
                            //context.Trace.Warn("Deflate Compression on");
                        }
                    }
                }*/

                if (filter.Action == "getchargepoints" || filter.Action == "poi")
                {
                    System.Diagnostics.Debug.WriteLine("At getpoilist output: " + stopwatch.ElapsedMilliseconds + "ms");
                    await OutputPOIList(outputProvider, context, filter);
                }

                if (filter.Action == "getcompactpoilist")
                {
                    //experimental::
                    await OutputCompactPOIList(context, filter);
                }

                if (filter.Action == "getcorereferencedata")
                {
                    await OutputCoreReferenceData(outputProvider, context, filter);
                }

                if (filter.Action == "geocode")
                {
                    OutputGeocodingResult(outputProvider, context, filter);
                }

                if (filter.Action == "availability")
                {
                    OutputAvailabilityResult(outputProvider, context, filter);
                }

                if (filter.Action == "profile.authenticate")
                {
                    await OutputProfileSignInResult(outputProvider, context, filter);
                }

                if (filter.Action == "profile.register")
                {
                    await OutputProfileRegisterResult(outputProvider, context, filter);
                }

                if (filter.Action == "refreshmirror")
                {
                    try
                    {
                        var itemsUpdated = await OCM.Core.Data.CacheManager.RefreshCachedData();
                        await new JSONOutputProvider().GetOutput(context, context.Response.Body, new { POICount = itemsUpdated, Status = "OK" }, filter);
                    }
                    catch (Exception exp)
                    {
                        await new JSONOutputProvider().GetOutput(context, context.Response.Body, new { Status = "Error", Message = exp.ToString() }, filter);
                    }
                }

                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine("Total output time: " + stopwatch.ElapsedMilliseconds + "ms");
            }
        }

        /// <summary>
        /// Output standard POI List results
        /// </summary>
        /// <param name="outputProvider"></param>
        /// <param name="context"></param>
        /// <param name="filter"></param>
        private async Task OutputPOIList(IOutputProvider outputProvider, HttpContext context, APIRequestParams filter)
        {
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            List<OCM.API.Common.Model.ChargePoint> dataList = null;

            //get list of charge points for output:
            var poiManager = new POIManager();
            dataList = poiManager.GetPOIList(filter);

            if (dataList != null && filter.LevelOfDetail > 1 && dataList.Count <= filter.MaxResults)
            {
                // level of detail filter was supplied but we have less than our default limit of results, repeat query with full level of detail
                filter.LevelOfDetail = 1;
                dataList = poiManager.GetPOIList(filter);
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine("OutputPOIList: Time for Query/Conversion: " + stopwatch.ElapsedMilliseconds + "ms");
            stopwatch.Restart();
#endif
            //send response
            await outputProvider.GetOutput(context, context.Response.Body, dataList, filter);

#if DEBUG
            System.Diagnostics.Debug.WriteLine("OutputPOIList: Time for Output to stream: " + stopwatch.ElapsedMilliseconds + "ms");
#endif
        }

        /// <summary>
        /// Output compact (id's only for reference data) POI list
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filter"></param>
        private async Task OutputCompactPOIList(HttpContext context, APIRequestParams filter)
        {
            //get list of charge points as compact POISearchResult List for output:
            List<OCM.API.Common.Model.ChargePoint> dataList = new POIManager().GetPOIList(filter);
            int numResults = dataList.Count;

            List<POISearchResult> poiList = new List<POISearchResult>();
            foreach (var c in dataList)
            {
                var poi = new POISearchResult
                {
                    ID = c.ID,
                    Title = c.AddressInfo.Title,
                    Address = c.AddressInfo.AddressLine1,
                    Distance = c.AddressInfo.Distance,
                    Latitude = (double)c.AddressInfo.Latitude,
                    Longitude = (double)c.AddressInfo.Longitude,
                    Postcode = c.AddressInfo.Postcode,
                    UsageTypeID = c.UsageType != null ? c.UsageType.ID : 0,
                    StatusTypeID = c.StatusType != null ? c.StatusType.ID : 0
                };
                poiList.Add(poi);
            }
            var o = new JSONOutputProvider();
            string description = "Compact POI List: id=ID, t= Title, u= UsageTypeID, s= StatusTypeID, a=Address, p=Postcode, lt=Latitude, lg=Longitude, d= Distance";
            await o.PerformSerialisationV2(context.Response.Body, new { status = "OK", description = description, count = poiList.Count, results = poiList }, filter.Callback);
        }

        /// <summary>
        /// Output Core Reference Data (lookup lists, default objects)
        /// </summary>
        /// <param name="outputProvider"></param>
        /// <param name="context"></param>
        /// <param name="filter"></param>
        private async Task OutputCoreReferenceData(IOutputProvider outputProvider, HttpContext context, APIRequestParams filter)
        {
            //get core reference data
            var refDataManager = new ReferenceDataManager();
            CoreReferenceData data = null;

            data = refDataManager.GetCoreReferenceData(filter);

            /*
            //cache result
            if (context.Cache["CoreRefData"] != null && filter.EnableCaching)
            {
                data = (CoreReferenceData)context.Cache["CoreRefData"];
            }
            else
            {
               

                context.Cache.Add("CoreRefData", data, null, Cache.NoAbsoluteExpiration, new TimeSpan(1, 0, 0), CacheItemPriority.Normal, null);
            }*/

            //populate non-cached fragments (user profile)
            data.UserProfile = new InputProviderBase().GetUserFromAPICall(context);

            //send response
            await outputProvider.GetOutput(context, context.Response.Body, data, filter);
        }

        private void OutputAvailabilityResult(IOutputProvider outputProvider, HttpContext context, APIRequestParams filter)
        {
            //TODO: provider specific availability check with result caching
            var results = new List<object>();

            foreach (var poiId in filter.ChargePointIDs)
            {
                results.Add(new { id = filter.ChargePointIDs, status = StandardStatusTypes.Unknown, timestamp = DateTime.UtcNow });
            }
            //send API response
            if (filter.IsEnvelopedResponse)
            {
                var responseEnvelope = new APIResponseEnvelope();
                responseEnvelope.Data = results;
                outputProvider.GetOutput(context, context.Response.Body, responseEnvelope, filter);
            }
            else
            {
                outputProvider.GetOutput(context, context.Response.Body, results, filter);
            }
        }

        private async Task OutputProfileSignInResult(IOutputProvider outputProvider, HttpContext context, APIRequestParams filter)
        {
            var sr = new System.IO.StreamReader(context.Request.Body);
            string jsonContent = await sr.ReadToEndAsync();
            var loginModel = JsonConvert.DeserializeObject<LoginModel>(jsonContent);

            User user = new OCM.API.Common.UserManager().GetUser(loginModel);
            string access_token = null;
            var responseEnvelope = new APIResponseEnvelope();
            if (user == null)
            {
                context.Response.StatusCode = 401;
                return;
            }
            else
            {
                access_token = Security.JWTAuth.GenerateEncodedJWT(user);

                /*
                 var validatedToken = Security.JWTAuthTicket.ValidateJWTForUser(testTicket, user);
                 */
            }

            responseEnvelope.Data = new { UserProfile = user, access_token = access_token };

            await outputProvider.GetOutput(context, context.Response.Body, responseEnvelope, filter);
        }

        private async Task OutputProfileRegisterResult(IOutputProvider outputProvider, HttpContext context, APIRequestParams filter)
        {
            var sr = new System.IO.StreamReader(context.Request.Body);
            string jsonContent = sr.ReadToEnd();
            var registration = JsonConvert.DeserializeObject<RegistrationModel>(jsonContent);

            User user = null;

            if (!string.IsNullOrEmpty(registration.EmailAddress) && registration.EmailAddress.Trim().Length > 5 && registration.EmailAddress.Contains("@"))
            {
                if (!string.IsNullOrWhiteSpace(registration.Password) && registration.Password.Trim().Length > 4)
                {
                    user = new OCM.API.Common.UserManager().RegisterNewUser(registration);
                }
            }
            else
            {
                context.Response.StatusCode = 401;
            }

            string access_token = null;
            var responseEnvelope = new APIResponseEnvelope();

            if (user != null)
            {
                context.Response.StatusCode = 401;
                return;
            }
            else
            {
                access_token = Security.JWTAuth.GenerateEncodedJWT(user);
            }

            responseEnvelope.Data = new { UserProfile = user, access_token = access_token };

            await outputProvider.GetOutput(context, context.Response.Body, responseEnvelope, filter);
        }

        private void OutputGeocodingResult(IOutputProvider outputProvider, HttpContext context, APIRequestParams filter)
        {
            GeocodingResult result = null;

            //get or get and cache result
            /*if (context.Cache["Geocoding_" + filter.HashKey] != null && filter.EnableCaching)
            {
                result = (GeocodingResult)context.Cache["Geocoding_" + filter.HashKey];
            }
            else
            {
               
                
                //context.Cache.Add("Geocoding_" + filter.HashKey, result, null, Cache.NoAbsoluteExpiration, new TimeSpan(1, 0, 0), CacheItemPriority.Normal, null);
            }*/

            var geocoder = new GeocodingHelper();
            geocoder.IncludeExtendedData = false;

            if (!string.IsNullOrEmpty(filter.Address))
            {
                result = geocoder.GeolocateAddressInfo_MapquestOSM(filter.Address);
            }
            else if (filter.Latitude != null && filter.Longitude != null)
            {
                result = geocoder.ReverseGecode_MapquestOSM((double)filter.Latitude, (double)filter.Longitude, new ReferenceDataManager());
            }

            //send API response
            if (filter.IsEnvelopedResponse)
            {
                var responseEnvelope = new APIResponseEnvelope();
                responseEnvelope.Data = result;
                outputProvider.GetOutput(context, context.Response.Body, responseEnvelope, filter);
            }
            else
            {
                outputProvider.GetOutput(context, context.Response.Body, result, filter);
            }
        }

        private void SetAPIDefaults(HttpContext context)
        {
            // determine default api bebaviours based on the expected settings for each api version
            APIBehaviourVersion = 1;

            if (context.Request.Path.ToString().StartsWith("/v2/"))
            {
                APIBehaviourVersion = 2;
                this.DefaultAction = "nop";
            }

            if (context.Request.Path.ToString().StartsWith("/v3/"))
            {
                APIBehaviourVersion = 3;
                this.DefaultAction = "nop";
            }

            if (context.Request.Path.ToString().Contains("/referencedata"))
            {
                this.APIBehaviourVersion = 3;
                this.DefaultAction = "getcorereferencedata";
                this.IsQueryByPost = false;
            }

            if (context.Request.Path.ToString().Contains("/poi"))
            {
                this.APIBehaviourVersion = 3;
                this.DefaultAction = "poi";
                this.IsQueryByPost = false;
            }

            if (context.Request.Path.ToString().Contains("profile/authenticate"))
            {
                this.APIBehaviourVersion = 3;
                this.DefaultAction = "profile.authenticate";
                this.IsQueryByPost = true;
            }

            if (context.Request.Path.ToString().Contains("comment"))
            {
                this.APIBehaviourVersion = 3;
                this.DefaultAction = "comment";
                this.IsQueryByPost = false;
            }

            if (context.Request.Path.ToString().Contains("mediaitem"))
            {
                this.APIBehaviourVersion = 3;
                this.DefaultAction = "mediaitem";
                this.IsQueryByPost = false;
            }

            if (context.Request.Path.ToString().Contains("geocode"))
            {
                this.APIBehaviourVersion = 3;
                this.DefaultAction = "geocode";
                this.IsQueryByPost = false;
            }


        }

        /// <summary>
        /// Main entry point for API calls
        /// </summary>
        /// <param name="context"></param>
        public async Task ProcessRequest(HttpContext context)
        {

            SetAPIDefaults(context);

            ClearResponse(context);

#if DEBUG
            //   context.Response.AddHeader("Access-Control-Allow-Origin", "*");
#endif
            if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
            {
                SetAllowCrossSiteRequestOrigin(context);
            }
            context.Response.Headers.TryAdd("Allow", "POST,GET,PUT,OPTIONS");

            if (!String.IsNullOrEmpty(context.Request.Headers["Access-Control-Request-Headers"]))
            {
                context.Response.Headers.TryAdd("Access-Control-Allow-Headers", context.Request.Headers["Access-Control-Request-Headers"]);
            }

            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                //context.ApplicationInstance.CompleteRequest();
                return;
            }
            else
            {
                SetNoCacheHeaders(context);
            }

            //decide if we are processing a query or a write (post). Some queries are delivered by POST instead of GET.
            if (context.Request.Method == "POST" && !IsQueryByPost)
            {
                //process input request
                await PerformInput(context);

                //flush output caching (because a change has been applied)
                /*var cacheEnum = context.Cache.GetEnumerator();
                while (cacheEnum.MoveNext())
                {
                    context.Cache.Remove(cacheEnum.Key.ToString());
                }*/
            }
            else
            {
                await PerformOutput(context);
            }

            //context.ApplicationInstance.CompleteRequest();
        }

        private void SetAllowCrossSiteRequestOrigin(HttpContext context)
        {
            string origin = context.Request.Headers["Origin"];
            if (!String.IsNullOrEmpty(origin))
                //You can make some sophisticated checks here
                context.Response.Headers.TryAdd("Access-Control-Allow-Origin", origin);
            else
                //This is necessary for Chrome/Safari actual request
                context.Response.Headers.TryAdd("Access-Control-Allow-Origin", "*");
        }

        protected void SetNoCacheHeaders(HttpContext context)
        {
            // FIXME:
            /*
            context.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            context.Response.Cache.SetValidUntilExpires(false);
            context.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetNoStore();
            */
        }

    }
}

