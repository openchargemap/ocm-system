using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Objects.DataClasses;
using OCM.Core.Data;
using OCM.API.Common.Model;
using KellermanSoftware.CompareNetObjects;
using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common
{

    public class ChargePointManager
    {
        public bool LoadUserComments = false;

        public ChargePointManager()
        {
            LoadUserComments = false;
        }

        public Model.ChargePoint Get(int id)
        {
            return this.Get(id, false);
        }

        public Model.ChargePoint Get(int id, bool includeComments)
        {
            OCMEntities dataModel = new OCMEntities();
            var item = dataModel.ChargePoints.FirstOrDefault(c => c.ID == id);
            return Model.Extensions.ChargePoint.FromDataModel(item, includeComments);
        }

        [EdmFunction("OCM.Core.Data.OCMEntities.Store", "udf_GetDistanceFromLatLonKM")]
        public static double? GetDistanceFromLatLonKM(double? Latitude1, double? Longitude1, double? Latitude2, double? Longitude2)
        {
            //implements dummy call for entity framework mapping to corresponding SQL function
            throw new NotSupportedException("Direct calls are not supported.");

        }

        /// <summary>
        /// For given query/output settings, return list of charge points. May be a cached response.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public List<Model.ChargePoint> GetChargePoints(SearchFilterSettings settings)
        {
            string cacheKey = settings.HashKey;
            List<Model.ChargePoint> dataList = null;

            if (HttpContext.Current.Cache[cacheKey] == null || settings.EnableCaching == false)
            {
                int maxResults = settings.MaxResults;
                this.LoadUserComments = settings.IncludeComments;
                bool requiresDistance = false;

                if (settings.Latitude != null && settings.Longitude != null)
                {
                    requiresDistance = true;
                    maxResults = 10000; //TODO find way to prefilter on distance.
                }

                dataList = new List<Model.ChargePoint>();
                OCMEntities dataModel = new OCMEntities();

                //if distance filter provided in miles, convert to KM before use
                if (settings.DistanceUnit == Model.DistanceUnit.Miles && settings.Distance != null)
                {
                    settings.Distance = GeoManager.ConvertMilesToKM((double)settings.Distance);
                }

                bool filterByConnectionTypes = false;
                bool filterByLevels = false;
                bool filterByOperators = false;
                bool filterByCountries = false;
                bool filterByUsage = false;
                bool filterByStatus = false;
                bool filterByDataProvider = false;

                if (settings.ConnectionTypeIDs != null) { filterByConnectionTypes = true; }
                else { settings.ConnectionTypeIDs = new int[] { -1 }; }

                if (settings.LevelIDs != null) { filterByLevels = true; }
                else { settings.LevelIDs = new int[] { -1 }; }

                if (settings.OperatorIDs != null) { filterByOperators = true; }
                else { settings.OperatorIDs = new int[] { -1 }; }

                if (settings.CountryIDs != null) { filterByCountries = true; }
                else { settings.CountryIDs = new int[] { -1 }; }

                if (settings.UsageTypeIDs != null) { filterByUsage = true; }
                else { settings.UsageTypeIDs = new int[] { -1 }; }

                if (settings.StatusTypeIDs != null) { filterByStatus = true; }
                else { settings.StatusTypeIDs = new int[] { -1 }; }

                if (settings.DataProviderIDs != null) { filterByDataProvider = true; }
                else { settings.DataProviderIDs = new int[] { -1 }; }

                //compile initial list of locations
                var chargePointList = from c in dataModel.ChargePoints
                                      where
                                          c.ParentChargePointID == null //exclude under review and delisted charge points
                                          && (c.AddressInfo != null && c.AddressInfo.Latitude != null && c.AddressInfo.Longitude != null)
                                          && ((settings.SubmissionStatusTypeID == null && (c.SubmissionStatusTypeID == null || c.SubmissionStatusTypeID == 100 || c.SubmissionStatusTypeID == 200))
                                                || (settings.SubmissionStatusTypeID == 0) //return all regardless of status
                                                || (settings.SubmissionStatusTypeID != null && c.SubmissionStatusTypeID == settings.SubmissionStatusTypeID)
                                                ) //by default return live cps only, otherwise use specific submission statusid
                                          && (settings.ChargePointID == null || c.ID == settings.ChargePointID)
                                          && (settings.CountryCode == null || c.AddressInfo.Country.ISOCode == settings.CountryCode)
                                          && (settings.OperatorName == null || c.Operator.Title == settings.OperatorName)
                                          && (settings.DataProviderName == null || c.DataProvider.Title == settings.DataProviderName)
                                          && (settings.LocationTitle == null || c.AddressInfo.Title.Contains(settings.LocationTitle))
                                          && (settings.ConnectionType == null || c.Connections.Count(conn => conn.ConnectionType.Title == settings.ConnectionType) > 0)
                                          //&& (settings.FastChargeOnly == false || c.ChargerTypes.Count(chgt => chgt.IsFastChargeCapable == true) > 0)
                                          && (settings.MinPowerKW == null || c.Connections.Count(conn => conn.PowerKW >= settings.MinPowerKW) > 0)
                                          && (filterByCountries == false || (filterByCountries == true && settings.CountryIDs.Contains((int)c.AddressInfo.CountryID)))
                                          && (filterByConnectionTypes == false || (filterByConnectionTypes == true && c.Connections.Count(conn => settings.ConnectionTypeIDs.Contains(conn.ConnectionType.ID)) > 0))
                                          && (filterByLevels == false || (filterByLevels == true && c.Connections.Count(chg => settings.LevelIDs.Contains((int)chg.ChargerType.ID)) > 0))
                                          && (filterByOperators == false || (filterByOperators == true && settings.OperatorIDs.Contains((int)c.OperatorID)))
                                          && (filterByUsage == false || (filterByUsage == true && settings.UsageTypeIDs.Contains((int)c.UsageTypeID)))
                                          && (filterByStatus == false || (filterByStatus == true && settings.StatusTypeIDs.Contains((int)c.StatusTypeID)))
                                          && (filterByDataProvider == false || (filterByDataProvider == true && settings.DataProviderIDs.Contains((int)c.DataProviderID)))
                                      select c;

                //compute/filter by distance (if required)
                var filteredList = from c in chargePointList
                                   where
                                   (requiresDistance == false)
                                   ||
                                   (
                                       (requiresDistance == true
                                           && (c.AddressInfo.Latitude != null && c.AddressInfo.Longitude != null)
                                           && (settings.Latitude != null && settings.Longitude != null)
                                           && (settings.Distance == null ||
                                                    (settings.Distance != null &&
                                                        GetDistanceFromLatLonKM(settings.Latitude, settings.Longitude, c.AddressInfo.Latitude, c.AddressInfo.Longitude) <= settings.Distance
                                                    )
                                           )
                                       )
                                   )
                                   select new { c, DistanceKM = GetDistanceFromLatLonKM(settings.Latitude, settings.Longitude, c.AddressInfo.Latitude, c.AddressInfo.Longitude) };

                if (requiresDistance)
                {
                    //if distance was a required output, sort results by distance
                    filteredList = filteredList.OrderBy(d => d.DistanceKM).Take(settings.MaxResults);
                }
                else
                {
                    filteredList = filteredList.OrderByDescending(p => p.c.DateCreated);
                }
                
                foreach (var item in filteredList.Take(maxResults))
                {
                    Model.ChargePoint c = Model.Extensions.ChargePoint.FromDataModel(item.c, settings.IncludeComments);

                    if (requiresDistance && c.AddressInfo != null)
                    {
                        c.AddressInfo.Distance = item.DistanceKM;
                        if (settings.DistanceUnit == Model.DistanceUnit.Miles && c.AddressInfo.Distance != null) c.AddressInfo.Distance = GeoManager.ConvertKMToMiles(c.AddressInfo.Distance);
                        c.AddressInfo.DistanceUnit = settings.DistanceUnit;
                    }

                    if (settings.IsLegacyAPICall && !(settings.APIVersion >= 2))
                    {
                        //for legacy callers, produce artificial list of Charger items
                        if (c.Chargers == null || c.Chargers.Count == 0)
                        {
                            if (c.Connections != null)
                            {
                                var chargerList = new List<Common.Model.ChargerInfo>();
                                foreach (var con in c.Connections)
                                {
                                    if (con.Level != null)
                                    {
                                        if (!chargerList.Exists(l => l.ChargerType == con.Level))
                                        {
                                            chargerList.Add(new Common.Model.ChargerInfo() { ChargerType = con.Level });
                                        }
                                    }
                                }
                                chargerList = chargerList.Distinct().ToList();
                                c.Chargers = chargerList;
                            }
                        }
                    }

                    if (c != null)
                    {
                        dataList.Add(c);
                    }
                }

                //cache results
                HttpContext.Current.Cache.Add(cacheKey, dataList, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 30, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);

            }
            else
            {
                dataList = (List<Model.ChargePoint>)HttpContext.Current.Cache[cacheKey];
            }

            return dataList.Take(settings.MaxResults).ToList();
        }

        /// <summary>
        /// for given charge point, return list of similar charge points based on location/title etc with approx similarity
        /// </summary>
        /// <param name="submission"></param>
        /// <returns></returns>
        public List<Model.ChargePoint> FindSimilar(Model.ChargePoint submission)
        {
            List<Model.ChargePoint> list = new List<Model.ChargePoint>();

            OCMEntities dataModel = new OCMEntities();

            //find similar locations (excluding same cp)
            var similarData = dataModel.ChargePoints.Where(c =>
                        c.ID != submission.ID
                        && c.ParentChargePointID == null //exclude under review and delisted charge points
                        && (c.SubmissionStatusTypeID == null || c.SubmissionStatusTypeID == 100 || c.SubmissionStatusTypeID == 200)
                        && (
                            c.AddressInfoID == submission.AddressInfo.ID
                            ||
                            c.AddressInfo.Postcode == submission.AddressInfo.Postcode
                            ||
                            c.AddressInfo.AddressLine1 == submission.AddressInfo.AddressLine1
                             ||
                            c.AddressInfo.Title == submission.AddressInfo.Title
                            )
                        );

            foreach (var item in similarData)
            {
                Model.ChargePoint c = Model.Extensions.ChargePoint.FromDataModel(item);

                if (c != null)
                {
                    int percentageSimilarity = 0;
                    if (c.AddressInfo.ID == submission.AddressInfo.ID) percentageSimilarity += 75;
                    if (c.AddressInfo.Postcode == submission.AddressInfo.Postcode) percentageSimilarity += 20;
                    if (c.AddressInfo.AddressLine1 == submission.AddressInfo.AddressLine1) percentageSimilarity += 50;
                    if (c.AddressInfo.Title == submission.AddressInfo.Title) percentageSimilarity += 25;
                    if (percentageSimilarity > 100) percentageSimilarity = 99;
                    c.PercentageSimilarity = percentageSimilarity;
                    list.Add(c);
                }
            }
            return list;
        }

        public static bool IsValid(Model.ChargePoint cp)
        {
            //determine if the basic CP details are valid as a submission or edit
            if (cp.AddressInfo == null) return false;
            if (String.IsNullOrEmpty(cp.AddressInfo.Title)) return false;

            if (cp.AddressInfo.Latitude == null && cp.AddressInfo.Longitude == null) return false;
            double lat = (double)cp.AddressInfo.Latitude;
            double lon = (double)cp.AddressInfo.Longitude;
            if (lat < -90 || lat > 90) return false;
            if (lon < -180 || lon > 180) return false;

            //otherwise, looks basically valid
            return true;
        }

        private static string GetDisplayName(Type dataType, string fieldName)
        {
            var attr = (DisplayAttribute)dataType.GetProperty(fieldName).GetCustomAttributes(typeof(DisplayAttribute), true).SingleOrDefault();

            if (attr == null)
            {
                var metadataType = (MetadataTypeAttribute)dataType.GetCustomAttributes(typeof(MetadataTypeAttribute), true).FirstOrDefault();
                if (metadataType != null)
                {
                    var property = metadataType.MetadataClassType.GetProperty(fieldName);
                    if (property != null)
                    {
                        attr = (DisplayAttribute)property.GetCustomAttributes(typeof(DisplayAttribute), true).SingleOrDefault();
                    }
                }
            }
            return (attr != null) ? attr.Name : String.Empty;
        }

        public bool HasDifferences(Model.ChargePoint poiA, Model.ChargePoint poiB)
        {
            var diffList = CheckDifferences(poiA, poiB);
            if (diffList.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public List<DiffItem> CheckDifferences(Model.ChargePoint poiA, Model.ChargePoint poiB)
        {
           
            var diffList = new List<DiffItem>();

            if (poiA == null && poiB == null)
            {
                return diffList;
            }

            if (poiA == null && poiB != null)
            {
                diffList.Add(new DiffItem { Context = "POI", ValueA = null, ValueB = "New" });
                return diffList;
            }

            if (poiB == null && poiA != null)
            {
                diffList.Add(new DiffItem { Context = "POI", ValueA = "New", ValueB = null });
                return diffList;
            }

            var objectComparison = new CompareObjects();
            objectComparison.CompareChildren = true;
            objectComparison.MaxDifferences = 1000;

            var exclusionList = new string[]
                {
                    "DateCreated",
                    "DateLastStatusUpdate",
                    ".DataProvider.ID",
                    ".DataProvider.WebsiteURL",
                    ".DataProvider.DataProviderStatusType",
                    ".StatusType.ID",
                    ".ConnectionType.FormalName",
                    ".Level.Comments",
                    ".AddressInfo.Distance",
                    ".AddressInfo.DistanceUnit",
                    ".AddressInfo.DistanceUnit.DistanceUnit",
                    ".SubmissionStatus.ID",
                    ".SubmissionStatus.IsLive"
                };
            objectComparison.ElementsToIgnore.AddRange(exclusionList);
            
            if (!objectComparison.Compare(poiA, poiB))
            {
                //clean up differences we want to exclude
                foreach (var exclusionSuffix in exclusionList)
                {
                    objectComparison.Differences.RemoveAll(e => e.PropertyName.EndsWith(exclusionSuffix));
                }
                
                diffList.AddRange(objectComparison.Differences.Select(difference => new DiffItem {Context = difference.PropertyName, ValueA = difference.Object1Value, ValueB = difference.Object2Value}));
            }
            return diffList;
        }
    }

}