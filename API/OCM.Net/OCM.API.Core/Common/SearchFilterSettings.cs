using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace OCM.API.Common
{
    public class APIRequestParams : ServiceParameterParser
    {
        private const int MAX_API_VERSION = 2;

        //result filters
        public int? APIVersion { get; set; }

        public string APIKey { get; set; }

        public string Action { get; set; }

        public string DataProviderName { get; set; }

        public string OperatorName { get; set; }

        public string LocationTitle { get; set; }

        public string Address { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public double? Distance { get; set; }

        public DistanceUnit DistanceUnit { get; set; }

        public string CountryCode { get; set; }

        public string ConnectionType { get; set; }

        public int? SubmissionStatusTypeID { get; set; }

        public int MaxResults { get; set; }

        public bool EnableCaching { get; set; }

        public bool IncludeComments { get; set; }

        public bool IsVerboseOutput { get; set; }

        public bool IsCompactOutput { get; set; }

        public bool IsCamelCaseOutput { get; set; }

        public bool IsEnvelopedResponse { get; set; }

        public bool AllowMirrorDB { get; set; } //if true, api will try to use mongodb to read

        public bool? IsOpenData { get; set; }

        public string Callback { get; set; }

        public double? MinPowerKW { get; set; }

        /// <summary>
        /// list of (lat,lng) points to search along (route etc)
        /// </summary>
        public List<LatLon> Polyline { get; set; }

        /// <summary>
        /// pair of top left and bottom right coords for bounding box. If specified then latitude/longitude params are ignored, distance is instead from box centre
        /// </summary>
        public List<LatLon> BoundingBox { get; set; }

        /// <summary>
        /// list of (lat,lng) points comprising the area to search within, for instance a region shape
        /// </summary>
        public List<LatLon> Polygon { get; set; }

        public int[] ChargePointIDs { get; set; }

        public int[] ConnectionTypeIDs { get; set; }

        public int[] OperatorIDs { get; set; }

        public int[] DataProviderIDs { get; set; }

        public int[] CountryIDs { get; set; }

        public int[] LevelIDs { get; set; }

        public int[] UsageTypeIDs { get; set; }

        public int[] StatusTypeIDs { get; set; }

        public DateTime? ChangesFromDate { get; set; }

        public DateTime? CreatedFromDate { get; set; }

        /// <summary>
        /// If supplied and greater than 1 the API will return a random sample of matching results, the higher the value the less results returned.
        /// </summary>
        public int? LevelOfDetail { get; set; }

        #region deprecated api filters

        public bool FastChargeOnly { get; set; }

        #endregion deprecated api filters

        public APIRequestParams()
        {
            DistanceUnit = DistanceUnit.Miles;
            MaxResults = 100;
            APIVersion = null;
            IsVerboseOutput = true;
            IsCompactOutput = false;
            AllowMirrorDB = true;
        }

        public bool IsLegacyAPICall
        {
            get
            {
                if (APIVersion == null || APIVersion < MAX_API_VERSION)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void ParseParameters(HttpContext context)
        {
            this.ParseParameters(this, context);
        }

        public void ParseParameters(APIRequestParams settings, HttpContext context)
        {
            //get parameter values if any
            if (!String.IsNullOrEmpty(context.Request["v"])) settings.APIVersion = ParseInt(context.Request["v"]);
            if (settings.APIVersion > MAX_API_VERSION) settings.APIVersion = MAX_API_VERSION;

            if (!String.IsNullOrEmpty(context.Request["action"])) settings.Action = ParseString(context.Request["action"]);

            if (!String.IsNullOrEmpty(context.Request["apikey"])) settings.APIKey = ParseString(context.Request["apikey"]);
            if (!String.IsNullOrEmpty(context.Request["chargepointid"])) settings.ChargePointIDs = ParseIntList(context.Request["chargepointid"]);
            if (!String.IsNullOrEmpty(context.Request["operatorname"])) settings.OperatorName = ParseString(context.Request["operatorname"]);
            if (!String.IsNullOrEmpty(context.Request["operatorid"])) settings.OperatorIDs = ParseIntList(context.Request["operatorid"]);
            if (!String.IsNullOrEmpty(context.Request["connectiontypeid"])) settings.ConnectionTypeIDs = ParseIntList(context.Request["connectiontypeid"]);
            if (!String.IsNullOrEmpty(context.Request["countryid"])) settings.CountryIDs = ParseIntList(context.Request["countryid"]);
            if (!String.IsNullOrEmpty(context.Request["levelid"])) settings.LevelIDs = ParseIntList(context.Request["levelid"]);
            if (!String.IsNullOrEmpty(context.Request["usagetypeid"])) settings.UsageTypeIDs = ParseIntList(context.Request["usagetypeid"]);
            if (!String.IsNullOrEmpty(context.Request["statustypeid"])) settings.StatusTypeIDs = ParseIntList(context.Request["statustypeid"]);
            if (!String.IsNullOrEmpty(context.Request["dataproviderid"])) settings.DataProviderIDs = ParseIntList(context.Request["dataproviderid"]);
            if (!String.IsNullOrEmpty(context.Request["opendata"])) settings.IsOpenData = ParseBoolNullable(context.Request["opendata"]);

            if (!String.IsNullOrEmpty(context.Request["minpowerkw"])) settings.MinPowerKW = ParseDouble(context.Request["minpowerkw"]);

            if (!String.IsNullOrEmpty(context.Request["dataprovidername"])) settings.DataProviderName = ParseString(context.Request["dataprovidername"]);
            if (!String.IsNullOrEmpty(context.Request["locationtitle"])) settings.LocationTitle = ParseString(context.Request["locationtitle"]);
            if (!String.IsNullOrEmpty(context.Request["address"])) settings.Address = ParseString(context.Request["address"]);
            if (!String.IsNullOrEmpty(context.Request["countrycode"])) settings.CountryCode = ParseString(context.Request["countrycode"]);

            if (!String.IsNullOrEmpty(context.Request["latitude"]))
            {
                settings.Latitude = ParseDouble(context.Request["latitude"]);
                if (settings.Latitude < -90 || settings.Latitude > 90) settings.Latitude = null;
            }

            if (!String.IsNullOrEmpty(context.Request["longitude"]))
            {
                settings.Longitude = ParseDouble(context.Request["longitude"]);
                if (settings.Longitude < -180 || settings.Longitude > 180) settings.Longitude = null;
            }

            if (!String.IsNullOrEmpty(context.Request["distance"])) settings.Distance = ParseDouble(context.Request["distance"]);
            if (!String.IsNullOrEmpty(context.Request["distanceunit"])) settings.DistanceUnit = ParseDistanceUnit(context.Request["distanceunit"]);

            if (!String.IsNullOrEmpty(context.Request["polyline"])) settings.Polyline = ParsePolyline(context.Request["polyline"]);
            //e.g. |k~aEm|bbUewBx}C_kEnyCi}Edn@d\ssFb\kyN
            //settings.Polyline = ParsePolyline("_p~iF~ps|U_ulLnnqC_mqNvxq`@");
            //settings.Polyline = ParsePolyline("(38.5, -120.2), (40.7, -120.95), (43.252, -126.453)");

            if (!String.IsNullOrEmpty(context.Request["boundingbox"])) settings.BoundingBox = ParseBoundingBox(context.Request["boundingbox"]);     //e.g. (-31.947017,115.803974),(-31.956672,115.819968)
            if (!String.IsNullOrEmpty(context.Request["polygon"])) settings.Polygon = ParsePolygon(context.Request["polygon"], true);

            if (!String.IsNullOrEmpty(context.Request["connectiontype"])) settings.ConnectionType = ParseString(context.Request["connectiontype"]);
            if (!String.IsNullOrEmpty(context.Request["submissionstatustypeid"])) settings.SubmissionStatusTypeID = ParseInt(context.Request["submissionstatustypeid"]);
            if (!String.IsNullOrEmpty(context.Request["modifiedsince"])) settings.ChangesFromDate = ParseDate(context.Request["modifiedsince"]);

            if (!String.IsNullOrEmpty(context.Request["levelofdetail"])) settings.LevelOfDetail = ParseInt(context.Request["levelofdetail"]);
            if (!String.IsNullOrEmpty(context.Request["enablecaching"])) settings.EnableCaching = ParseBool(context.Request["enablecaching"], true);
            if (!String.IsNullOrEmpty(context.Request["allowmirror"])) settings.AllowMirrorDB = ParseBool(context.Request["allowmirror"], false);
            if (!String.IsNullOrEmpty(context.Request["includecomments"])) settings.IncludeComments = ParseBool(context.Request["includecomments"], false);
            if (!String.IsNullOrEmpty(context.Request["verbose"])) settings.IsVerboseOutput = ParseBool(context.Request["verbose"], false);
            if (!String.IsNullOrEmpty(context.Request["compact"])) settings.IsCompactOutput = ParseBool(context.Request["compact"], false);
            if (!String.IsNullOrEmpty(context.Request["camelcase"])) settings.IsCamelCaseOutput = ParseBool(context.Request["camelcase"], false);
            if (!String.IsNullOrEmpty(context.Request["callback"])) settings.Callback = ParseString(context.Request["callback"]);

            if (!String.IsNullOrEmpty(context.Request["maxresults"]))
            {
                try
                {
                    settings.MaxResults = int.Parse(context.Request["maxresults"]);
                }
                catch (Exception)
                {
                    settings.MaxResults = 100;
                }
            }

            //default action
            if (String.IsNullOrEmpty(settings.Action))
            {
                settings.Action = "getchargepoints";
            }

            //handle deprecated items
            if (settings.APIVersion == null || settings.APIVersion == 1)
            {
                if (!String.IsNullOrEmpty(context.Request["fastchargeonly"])) settings.FastChargeOnly = ParseBool(context.Request["fastchargeonly"], false);
            }
        }

        /// <summary>
        /// Uses the search filters to create a simple cache key for the returned output
        /// </summary>
        public string HashKey
        {
            get
            {
                //return this.GetHashCode().ToString();

                string key = "";

                if (APIKey != null) key += ":" + APIKey;
                if (Action != null) key += ":" + Action;
                if (DataProviderName != null) key += ":" + DataProviderName;
                if (OperatorName != null) key += ":" + OperatorName;
                if (LocationTitle != null) key += ":" + LocationTitle;
                if (Address != null) key += ":" + Address;
                if (Latitude != null) key += ":" + Latitude.ToString();
                if (Longitude != null) key += ":" + Longitude.ToString();
                if (Distance != null) key += ":" + Distance.ToString();
                key += ":" + DistanceUnit.ToString();
                if (CountryCode != null) key += ":" + CountryCode;
                if (ConnectionType != null) key += ":" + ConnectionType;
                key += ":" + FastChargeOnly.ToString();
                if (SubmissionStatusTypeID != null) key += ":" + SubmissionStatusTypeID.ToString();
                key += ":" + MaxResults.ToString();
                key += ":" + IncludeComments.ToString();
                key += ":" + this.IsCompactOutput.ToString();
                if (ChargePointIDs != null) key += ":cp_id:" + IntArrayToString(ChargePointIDs);
                if (CountryIDs != null) key += ":c_id:" + IntArrayToString(CountryIDs);
                if (LevelIDs != null) key += ":l_id:" + IntArrayToString(LevelIDs);
                if (ConnectionTypeIDs != null) key += ":ct_id:" + IntArrayToString(ConnectionTypeIDs);
                if (OperatorIDs != null) key += ":op_id:" + IntArrayToString(OperatorIDs);
                if (UsageTypeIDs != null) key += ":usg_id:" + IntArrayToString(UsageTypeIDs);
                if (StatusTypeIDs != null) key += ":stat_id:" + IntArrayToString(StatusTypeIDs);
                if (DataProviderIDs != null) key += ":prov_id:" + IntArrayToString(DataProviderIDs);
                if (IsOpenData != null) key += ":opendata:" + IsOpenData.ToString();
                if (MinPowerKW != null) key += ":minpowerkw:" + MinPowerKW.ToString();
                if (Polyline != null) key += ":polyline:" + Polyline.GetHashCode().ToString();
                if (Polygon != null) key += ":polygon:" + Polygon.GetHashCode().ToString();
                if (BoundingBox != null) key += ":boundingbox:" + BoundingBox.GetHashCode().ToString();
                if (LevelOfDetail > 1) key += ":lod:" + LevelOfDetail.ToString();
                if (ChangesFromDate != null) key += ":modified:" + ChangesFromDate.Value.Ticks.ToString();

                return OCM.Core.Util.SecurityHelper.GetMd5Hash(key);
            }
        }

        public string IntArrayToString(int[] array)
        {
            string output = "{";
            foreach (int val in array)
            {
                output += val.ToString() + ",";
            }
            output += "}";
            return output;
        }
    }
}