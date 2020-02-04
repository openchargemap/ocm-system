using System;
using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.API.Common
{

    public class NullSafeDictionary<TKey, TValue> : Dictionary<TKey, TValue> where TValue : class
    {
        // https://stackoverflow.com/questions/14150508/how-to-get-null-instead-of-the-keynotfoundexception-accessing-dictionary-value-b/14150668#14150668
        public new TValue this[TKey key]
        {
            get
            {
                if (!ContainsKey(key))
                    return null;
                else
                    return base[key];
            }
            set
            {
                if (!ContainsKey(key))
                    Add(key, value);
                else
                    base[key] = value;
            }
        }
    }

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

        /// <summary>
        /// if true, api will try to use mongodb to query
        /// </summary>
        public bool AllowMirrorDB { get; set; }

        /// <summary>
        /// if true, api will try to use backing store database (SQL) to query
        /// </summary>
        public bool AllowDataStoreDB { get; set; }

        public bool? IsOpenData { get; set; }

        public string Callback { get; set; }

        public double? MinPowerKW { get; set; }
        public double? MaxPowerKW { get; set; }

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

        public string SortBy { get; set; }

        /// <summary>
        /// If supplied, results will be ID's greater than the given value, used for batch imports
        /// </summary>
        public int? GreaterThanId { get; set; }

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
            AllowDataStoreDB = true;
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

        public void ParseParameters(APIRequestParams settings, NullSafeDictionary<string,string> requestParams)
        {
            //get parameter values if any
            if (!String.IsNullOrEmpty(requestParams["v"])) settings.APIVersion = ParseInt(requestParams["v"]);
            if (settings.APIVersion > MAX_API_VERSION) settings.APIVersion = MAX_API_VERSION;

            if (!String.IsNullOrEmpty(requestParams["action"])) settings.Action = ParseString(requestParams["action"]);

            if (!String.IsNullOrEmpty(requestParams["apikey"])) settings.APIKey = ParseString(requestParams["apikey"]);

            // accept either id or chargepointid params
            if (!String.IsNullOrEmpty(requestParams["id"])) settings.ChargePointIDs = ParseIntList(requestParams["id"]);
            if (!String.IsNullOrEmpty(requestParams["chargepointid"])) settings.ChargePointIDs = ParseIntList(requestParams["chargepointid"]);

            if (!String.IsNullOrEmpty(requestParams["greaterthanid"])) settings.GreaterThanId = ParseInt(requestParams["greaterthanid"]);

            if (!String.IsNullOrEmpty(requestParams["operatorname"])) settings.OperatorName = ParseString(requestParams["operatorname"]);
            if (!String.IsNullOrEmpty(requestParams["operatorid"])) settings.OperatorIDs = ParseIntList(requestParams["operatorid"]);
            if (!String.IsNullOrEmpty(requestParams["connectiontypeid"])) settings.ConnectionTypeIDs = ParseIntList(requestParams["connectiontypeid"]);
            if (!String.IsNullOrEmpty(requestParams["countryid"])) settings.CountryIDs = ParseIntList(requestParams["countryid"]);
            if (!String.IsNullOrEmpty(requestParams["levelid"])) settings.LevelIDs = ParseIntList(requestParams["levelid"]);
            if (!String.IsNullOrEmpty(requestParams["usagetypeid"])) settings.UsageTypeIDs = ParseIntList(requestParams["usagetypeid"]);
            if (!String.IsNullOrEmpty(requestParams["statustypeid"])) settings.StatusTypeIDs = ParseIntList(requestParams["statustypeid"]);
            if (!String.IsNullOrEmpty(requestParams["dataproviderid"])) settings.DataProviderIDs = ParseIntList(requestParams["dataproviderid"]);
            if (!String.IsNullOrEmpty(requestParams["opendata"])) settings.IsOpenData = ParseBoolNullable(requestParams["opendata"]);

            if (!String.IsNullOrEmpty(requestParams["minpowerkw"]))
            {
                settings.MinPowerKW = ParseDouble(requestParams["minpowerkw"]);
                if (settings.MinPowerKW < 1) settings.MinPowerKW = null;
            }

            if (!String.IsNullOrEmpty(requestParams["maxpowerkw"]))
            {
                settings.MaxPowerKW = ParseDouble(requestParams["maxpowerkw"]);
                if (settings.MaxPowerKW < 1) settings.MaxPowerKW = null;
            }

            if (!String.IsNullOrEmpty(requestParams["dataprovidername"])) settings.DataProviderName = ParseString(requestParams["dataprovidername"]);
            if (!String.IsNullOrEmpty(requestParams["locationtitle"])) settings.LocationTitle = ParseString(requestParams["locationtitle"]);
            if (!String.IsNullOrEmpty(requestParams["address"])) settings.Address = ParseString(requestParams["address"]);
            if (!String.IsNullOrEmpty(requestParams["countrycode"])) settings.CountryCode = ParseString(requestParams["countrycode"]);

            if (!String.IsNullOrEmpty(requestParams["latitude"]))
            {
                settings.Latitude = ParseDouble(requestParams["latitude"]);
                if (settings.Latitude < -90 || settings.Latitude > 90) settings.Latitude = null;
            }

            if (!String.IsNullOrEmpty(requestParams["longitude"]))
            {
                settings.Longitude = ParseDouble(requestParams["longitude"]);
                if (settings.Longitude < -180 || settings.Longitude > 180) settings.Longitude = null;
            }

            if (!String.IsNullOrEmpty(requestParams["distance"])) settings.Distance = ParseDouble(requestParams["distance"]);
            if (!String.IsNullOrEmpty(requestParams["distanceunit"])) settings.DistanceUnit = ParseDistanceUnit(requestParams["distanceunit"]);

            if (!String.IsNullOrEmpty(requestParams["polyline"])) settings.Polyline = ParsePolyline(requestParams["polyline"]);
            //e.g. |k~aEm|bbUewBx}C_kEnyCi}Edn@d\ssFb\kyN
            //settings.Polyline = ParsePolyline("_p~iF~ps|U_ulLnnqC_mqNvxq`@");
            //settings.Polyline = ParsePolyline("(38.5, -120.2), (40.7, -120.95), (43.252, -126.453)");

            if (!String.IsNullOrEmpty(requestParams["boundingbox"])) settings.BoundingBox = ParseBoundingBox(requestParams["boundingbox"]);     //e.g. (-31.947017,115.803974),(-31.956672,115.819968)
            if (!String.IsNullOrEmpty(requestParams["polygon"])) settings.Polygon = ParsePolygon(requestParams["polygon"], true);

            if (!String.IsNullOrEmpty(requestParams["connectiontype"])) settings.ConnectionType = ParseString(requestParams["connectiontype"]);
            if (!String.IsNullOrEmpty(requestParams["submissionstatustypeid"])) settings.SubmissionStatusTypeID = ParseInt(requestParams["submissionstatustypeid"]);
            if (!String.IsNullOrEmpty(requestParams["modifiedsince"])) settings.ChangesFromDate = ParseDate(requestParams["modifiedsince"]);
            if (!String.IsNullOrEmpty(requestParams["createdsince"])) settings.CreatedFromDate = ParseDate(requestParams["createdsince"]);

            if (!String.IsNullOrEmpty(requestParams["levelofdetail"])) settings.LevelOfDetail = ParseInt(requestParams["levelofdetail"]);
            if (!String.IsNullOrEmpty(requestParams["enablecaching"])) settings.EnableCaching = ParseBool(requestParams["enablecaching"], true);
            if (!String.IsNullOrEmpty(requestParams["allowmirror"])) settings.AllowMirrorDB = ParseBool(requestParams["allowmirror"], false);
            if (!String.IsNullOrEmpty(requestParams["includecomments"])) settings.IncludeComments = ParseBool(requestParams["includecomments"], false);
            if (!String.IsNullOrEmpty(requestParams["verbose"])) settings.IsVerboseOutput = ParseBool(requestParams["verbose"], false);
            if (!String.IsNullOrEmpty(requestParams["compact"])) settings.IsCompactOutput = ParseBool(requestParams["compact"], false);
            if (!String.IsNullOrEmpty(requestParams["camelcase"])) settings.IsCamelCaseOutput = ParseBool(requestParams["camelcase"], false);
            if (!String.IsNullOrEmpty(requestParams["callback"])) settings.Callback = ParseString(requestParams["callback"]);

            if (!String.IsNullOrEmpty(requestParams["maxresults"]))
            {
                try
                {
                    settings.MaxResults = int.Parse(requestParams["maxresults"]);
                }
                catch (Exception)
                {
                    settings.MaxResults = 100;
                }
            }

            if (!String.IsNullOrEmpty(requestParams["sortby"]))
            {
                settings.SortBy = requestParams["sortby"].Trim().ToLower();
            }

            //default action
            if (String.IsNullOrEmpty(settings.Action))
            {
                settings.Action = "getchargepoints";
            }

            //handle deprecated items
            if (settings.APIVersion == null || settings.APIVersion == 1)
            {
                if (!String.IsNullOrEmpty(requestParams["fastchargeonly"])) settings.FastChargeOnly = ParseBool(requestParams["fastchargeonly"], false);
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