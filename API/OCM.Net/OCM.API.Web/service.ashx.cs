using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.API.Common.Model.Extended;
using OCM.API.InputProviders;
using OCM.API.OutputProviders;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Compression;
using System.Text;
using System.Web;
using System.Web.Caching;

namespace OCM.API
{
    /// <summary>
    /// Basic HTTP handler to parse parameters and stream response via various output type providers
    /// </summary>
    public class APICoreHTTPHandler : ServiceParameterParser, IHttpHandler
    {
        protected int APIBehaviourVersion { get; set; }

        protected string DefaultAction { get; set; }

        public APICoreHTTPHandler()
        {
            APIBehaviourVersion = 0;
            DefaultAction = null;
        }

        public bool IsRequestByRobot
        {
            get
            {
                var userAgent = HttpContext.Current.Request.UserAgent.ToLower();
                if (userAgent.Contains("robot") || userAgent.Contains("crawler") || userAgent.Contains("spider") || userAgent.Contains("slurp") || userAgent.Contains("googlebot") || userAgent.Contains("kml-google"))
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Handle input to API
        /// </summary>
        /// <param name="context"></param>
        private void PerformInput(HttpContext context)
        {
            OCM.API.InputProviders.IInputProvider inputProvider = null;
            bool performSubmissionCompletedRedirects = false;

            if (context.Request["format"] == "json")
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
                //gather input variables
                if (context.Request["action"] == "cp_submission")
                {
                    //gather/process data for submission
                    OCM.API.Common.Model.ChargePoint cp = new Common.Model.ChargePoint();

                    bool processedOK = inputProvider.ProcessEquipmentSubmission(context, ref cp);
                    bool submittedOK = false;

                    if (processedOK == true)
                    {
                        //perform submission

                        int submissionId = submissionManager.PerformPOISubmission(cp, user);
                        if (submissionId > -1) submittedOK = true;
                    }

                    if (processedOK && submittedOK)
                    {
                        if (performSubmissionCompletedRedirects)
                        {
                            if (submittedOK)
                            {
                                context.Response.Redirect("http://openchargemap.org/submissions/complete.aspx", true);
                            }
                            else
                            {
                                context.Response.Redirect("http://openchargemap.org/submissions/error.aspx", true);
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = 202;
                            //context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                        }
                    }
                    else
                    {
                        if (performSubmissionCompletedRedirects)
                        {
                            context.Response.Redirect("http://openchargemap.org/submissions/error.aspx", true);
                        }
                        else
                        {
                            context.Response.StatusCode = 500;
                            //context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                        }
                    }
                }

                if (context.Request["action"] == "comment_submission")
                {
                    UserComment comment = new UserComment();
                    bool processedOK = inputProvider.ProcessUserCommentSubmission(context, ref comment);
                    if (processedOK == true)
                    {
                        //perform submission
                        int result = submissionManager.PerformSubmission(comment, user);

                        if (result >= 0)
                        {
                            context.Response.Write("OK:" + result);
                        }
                        else
                        {
                            context.Response.Write("Error:" + result);
                        }
                    }
                    else
                    {
                        context.Response.Write("Error: Validation Failed");
                    }
                }

                if (context.Request["action"] == "contactus_submission")
                {
                    ContactSubmission contactSubmission = new ContactSubmission();
                    bool processedOK = inputProvider.ProcessContactUsSubmission(context, ref contactSubmission);

                    bool resultOK = submissionManager.SendContactUsMessage(contactSubmission.Name, contactSubmission.Email, contactSubmission.Comment);
                    if (resultOK == true)
                    {
                        context.Response.Write("OK");
                    }
                    else
                    {
                        context.Response.Write("Error");
                    }
                }

                if (context.Request["action"] == "mediaitem_submission")
                {
                    var p = inputProvider as OCM.API.InputProviders.HTMLFormInputProvider;
                    MediaItem m = new MediaItem();
                    bool accepted = false;
                    string msg = "";
                    try
                    {
                        accepted = p.ProcessMediaItemSubmission(context, ref m, user.ID);
                    }
                    catch (Exception exp)
                    {
                        msg += exp.ToString();
                    }
                    if (accepted)
                    {
                        submissionManager.PerformSubmission(m, user);
                        //OutputSubmissionReceivedMessage(context, "OK :" + m.ID, true);
                        context.Response.Write("OK");
                    }
                    else
                    {
                        //OutputBadRequestMessage(context, "Error, could not accept submission: " + msg);
                        context.Response.Write("Error");
                    }
                }
            }
        }

        private void OutputBadRequestMessage(HttpContext context, string message, int statusCode=400)
        {
            context.Response.StatusCode = statusCode;
            context.Response.Write("{\"status\":\"error\",\"description\":\"" + message + "\"}");
            context.Response.Flush();
            //context.Response.End();
        }

        private void OutputSubmissionReceivedMessage(HttpContext context, string message, bool processed)
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
                context.Response.Write("{\"status\":\"OK\",\"description\":\"" + message + "\"}");
                context.Response.End();
            } catch (Exception){
            
            }
        }

        /// <summary>
        /// Handle output from API
        /// </summary>
        /// <param name="context"></param>
        private async void PerformOutput(HttpContext context)
        {
            HttpContext.Current.Server.ScriptTimeout = 120; //max script execution time is 2 minutes

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

            filter.ParseParameters(context);

            //override ?v=2 etc if called via /api/v2/ or /api/v1
            if (APIBehaviourVersion > 0) filter.APIVersion = APIBehaviourVersion;
            if (APIBehaviourVersion >= 2) filter.Action = DefaultAction;

            if (context.Request.Url.Host.ToLower().StartsWith("api") && filter.APIVersion == null)
            {
                //API version is mandatory for api V2 onwards via api.openchargemap.* hostname
                OutputBadRequestMessage(context, "mandatory API Version not specified in request");
                return;
            }

            if (!String.IsNullOrEmpty(context.Request["output"]))
            {
                outputType = ParseString(context.Request["output"]);
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
                    OutputBadRequestMessage(context, "specified output type not supported in this API version");
                    return;
                }
            }

            if (IsRequestByRobot)
            {
                OutputBadRequestMessage(context, "API requests by robots are temporarily disabled.", statusCode:503);
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

                case "png":
                    outputProvider = new ImageOutputProvider();
                    break;

                default:
                    outputProvider = new XMLOutputProvider();
                    break;
            }

            if (outputProvider != null)
            {
                context.Response.ContentEncoding = Encoding.Default;
                context.Response.ContentType = outputProvider.ContentType;

                if (!(filter.Action == "getcorereferencedata" && String.IsNullOrEmpty(context.Request["SessionToken"])))
                {
                    //by default output is cacheable for 24 hrs, unless requested by a specific user.
                    context.Response.Cache.SetExpires(DateTime.Now.AddDays(1));
                    context.Response.Cache.SetCacheability(HttpCacheability.Public);
                    context.Response.Cache.SetValidUntilExpires(true);
                }

                if (ConfigurationManager.AppSettings["EnableOutputCompression"] == "true")
                {
                    //apply compression if accepted
                    string encodings = context.Request.Headers.Get("Accept-Encoding");

                    if (encodings != null)
                    {
                        encodings = encodings.ToLower();

                        if (encodings.ToLower().Contains("gzip"))
                        {
                            context.Response.Filter = new GZipStream(context.Response.Filter, CompressionLevel.Optimal, false);
                            context.Response.AppendHeader("Content-Encoding", "gzip");
                            //context.Trace.Warn("GZIP Compression on");
                        }
                        else
                        {
                            context.Response.Filter = new DeflateStream(context.Response.Filter, CompressionMode.Compress);
                            context.Response.AppendHeader("Content-Encoding", "deflate");
                            //context.Trace.Warn("Deflate Compression on");
                        }
                    }
                }
                if (filter.Action == "getchargepoints" || filter.Action == "getpoilist")
                {
                    OutputPOIList(outputProvider, context, filter);
                }

                if (filter.Action == "getcompactpoilist")
                {
                    //experimental::
                    OutputCompactPOIList(context, filter);
                }

                if (filter.Action == "getcorereferencedata")
                {
                    OutputCoreReferenceData(outputProvider, context, filter);
                }

                if (filter.Action == "geocode")
                {
                    this.OutputGeocodingResult(outputProvider, context, filter);
                }

                if (filter.Action == "availability")
                {
                    this.OutputAvailabilityResult(outputProvider, context, filter);
                }

                if (filter.Action == "refreshmirror")
                {
                    try
                    {
                        var itemsUpdated = await OCM.Core.Data.CacheManager.RefreshCachedData();
                        new JSONOutputProvider().GetOutput(context.Response.OutputStream, new { POICount = itemsUpdated, Status = "OK" }, filter);
                    }
                    catch (Exception exp)
                    {
                        new JSONOutputProvider().GetOutput(context.Response.OutputStream, new { Status = "Error", Message = exp.ToString() }, filter);
                    }
                }

                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine("Total output time: " + stopwatch.Elapsed.ToString());
            }
        }

        /// <summary>
        /// Output standard POI List results
        /// </summary>
        /// <param name="outputProvider"></param>
        /// <param name="context"></param>
        /// <param name="filter"></param>
        private void OutputPOIList(IOutputProvider outputProvider, HttpContext context, APIRequestParams filter)
        {
            List<OCM.API.Common.Model.ChargePoint> dataList = null;

            //get list of charge points for output:
            dataList = new POIManager().GetChargePoints(filter);

            int numResults = dataList.Count;

            //send response
            outputProvider.GetOutput(context.Response.OutputStream, dataList, filter);
        }

        /// <summary>
        /// Output compact (id's only for reference data) POI list
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filter"></param>
        private void OutputCompactPOIList(HttpContext context, APIRequestParams filter)
        {
            //get list of charge points as compact POISearchResult List for output:
            List<OCM.API.Common.Model.ChargePoint> dataList = new POIManager().GetChargePoints(filter);
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
            o.PerformSerialisationV2(context.Response.OutputStream, new { status = "OK", description = description, count = poiList.Count, results = poiList }, filter.Callback);
        }

        /// <summary>
        /// Output Core Reference Data (lookup lists, default objects)
        /// </summary>
        /// <param name="outputProvider"></param>
        /// <param name="context"></param>
        /// <param name="filter"></param>
        private void OutputCoreReferenceData(IOutputProvider outputProvider, HttpContext context, APIRequestParams filter)
        {
            //get core reference data
            var refDataManager = new ReferenceDataManager();
            CoreReferenceData data = null;

            //cache result
            if (HttpContext.Current.Cache["CoreRefData"] != null && filter.EnableCaching)
            {
                data = (CoreReferenceData)HttpContext.Current.Cache["CoreRefData"];
            }
            else
            {
                
                data = refDataManager.GetCoreReferenceData();
                
                HttpContext.Current.Cache.Add("CoreRefData", data, null, Cache.NoAbsoluteExpiration, new TimeSpan(1, 0, 0), CacheItemPriority.Normal, null);
            }

            //populate non-cached fragments (user profile)
            data.UserProfile = new InputProviderBase().GetUserFromAPICall(context);

            //send response
            outputProvider.GetOutput(context.Response.OutputStream, data, filter);
        }

        private void OutputAvailabilityResult(IOutputProvider outputProvider, HttpContext context, APIRequestParams filter)
        {
            //TODO: provider specific availability check with result caching
            var result = new {id= filter.ChargePointID, status = StandardStatusTypes.Unknown,timestamp=DateTime.UtcNow};

            //send API response
            if (filter.IsEnvelopedResponse)
            {
                var responseEnvelope = new APIResponseEnvelope();
                responseEnvelope.Data = result;
                outputProvider.GetOutput(context.Response.OutputStream, responseEnvelope, filter);
            }
            else
            {
                outputProvider.GetOutput(context.Response.OutputStream, result, filter);
            }
        }

        private void OutputGeocodingResult(IOutputProvider outputProvider, HttpContext context, APIRequestParams filter)
        {
            GeocodingResult result = null;

            //get or get and cache result
            if (HttpContext.Current.Cache["Geocoding_" + filter.HashKey] != null && filter.EnableCaching)
            {
                result = (GeocodingResult)HttpContext.Current.Cache["Geocoding_" + filter.HashKey];
            }
            else
            {
                var geocoder = new GeocodingHelper();
                geocoder.IncludeExtendedData = true;

                //result = geocoder.GeolocateAddressInfo_OSM(filter.Address);
                result = geocoder.GeolocateAddressInfo_Google(filter.Address);

                HttpContext.Current.Cache.Add("Geocoding_" + filter.HashKey, result, null, Cache.NoAbsoluteExpiration, new TimeSpan(1, 0, 0), CacheItemPriority.Normal, null);
            }

            //send API response
            if (filter.IsEnvelopedResponse)
            {
                var responseEnvelope = new APIResponseEnvelope();
                responseEnvelope.Data = result;
                outputProvider.GetOutput(context.Response.OutputStream, responseEnvelope, filter);
            }
            else
            {
                outputProvider.GetOutput(context.Response.OutputStream, result, filter);
            }
        }

        /// <summary>
        /// Main entry point for API calls
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod == "POST" && context.Request["action"].EndsWith("_submission"))
            {
                //process input request
                PerformInput(context);

                //flush output caching
                var cacheEnum = HttpContext.Current.Cache.GetEnumerator();
                while (cacheEnum.MoveNext())
                {
                    HttpContext.Current.Cache.Remove(cacheEnum.Key.ToString());
                }
            }
            else
            {
                PerformOutput(context);
            }
        }

        public bool IsReusable
        {
            get
            {
                //explicitly set to be reusable, on the assumption we don't hold state in this handler.
                return true;
            }
        }
    }
}