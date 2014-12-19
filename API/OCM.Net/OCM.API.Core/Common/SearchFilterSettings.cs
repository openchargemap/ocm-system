using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.Text;
using OCM.API.Common.Model;

namespace OCM.API.Common
{
    public class APIRequestSettings : ServiceParameterParser
    {
        private const int MAX_API_VERSION =2;

        //result filters
        public int? APIVersion { get; set; }
        public string APIKey { get; set; }
        public string Action { get; set; }
        public string DataProviderName { get; set; }
        public string OperatorName { get; set; }
        public string LocationTitle { get; set; }
        public string Address { get; set; }

        public int? ChargePointID { get; set; }
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
        public List<LatLon> Polyline { get; set; }

        public int[] ConnectionTypeIDs { get; set; }
        public int[] OperatorIDs { get; set; }
        public int[] DataProviderIDs { get; set; }
        public int[] CountryIDs { get; set; }
        public int[] LevelIDs { get; set; }
        public int[] UsageTypeIDs { get; set; }
        public int[] StatusTypeIDs { get; set; }

        public DateTime? ChangesFromDate { get; set; }

        #region deprecated api filters
        public bool FastChargeOnly { get; set; }
        #endregion

        public APIRequestSettings()
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
            get {
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

        public void ParseParameters(APIRequestSettings settings, HttpContext context)
        {
            //get parameter values if any
            if (!String.IsNullOrEmpty(context.Request["v"])) settings.APIVersion = ParseInt(context.Request["v"]);
            if (settings.APIVersion > MAX_API_VERSION) settings.APIVersion = MAX_API_VERSION;

            if (!String.IsNullOrEmpty(context.Request["action"])) settings.Action = ParseString(context.Request["action"]);
            
            if (!String.IsNullOrEmpty(context.Request["apikey"])) settings.APIKey = ParseString(context.Request["apikey"]);
            if (!String.IsNullOrEmpty(context.Request["chargepointid"])) settings.ChargePointID = ParseInt(context.Request["chargepointid"]);
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
            //settings.Polyline = ParsePolyline("_p~iF~ps|U_ulLnnqC_mqNvxq`@");
            //settings.Polyline = ParsePolyline("(38.5, -120.2), (40.7, -120.95), (43.252, -126.453)");

            if (!String.IsNullOrEmpty(context.Request["connectiontype"])) settings.ConnectionType = ParseString(context.Request["connectiontype"]);
            
            if (!String.IsNullOrEmpty(context.Request["submissionstatustypeid"])) settings.SubmissionStatusTypeID = ParseInt(context.Request["submissionstatustypeid"]);
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
                if (ChargePointID != null) key += ":" + ChargePointID.ToString();
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
                if (ChangesFromDate != null) key += ":modified:" +  ChangesFromDate.Value.Ticks.ToString();

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