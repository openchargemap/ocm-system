using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OCM.Core.Data;
using OCM.API.Common.Model;
using KellermanSoftware.CompareNetObjects;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using OCM.API.Common.Model.Extended;

namespace OCM.API.Common
{

    public class POIManager
    {
        public bool LoadUserComments = false;

        public POIManager()
        {
            LoadUserComments = false;
        }

        public Model.ChargePoint Get(int id)
        {
            return this.Get(id, false);
        }

        public Model.ChargePoint Get(int id, bool includeExtendedInfo)
        {
            var dataModel = new OCMEntities();
            var item = dataModel.ChargePoints.FirstOrDefault(c => c.ID == id);
            return Model.Extensions.ChargePoint.FromDataModel(item, includeExtendedInfo, includeExtendedInfo, includeExtendedInfo, true);
        }

        /// <summary>
        /// Populates extended properties (reference data etc) of a simple POI from data model, useful for previewing as a fully populated new/edited poi
        /// </summary>
        /// <param name="poi"></param>
        /// <returns></returns>
        public Model.ChargePoint PreviewPopulatedPOIFromModel(Model.ChargePoint poi)
        {
            var dataModel = new OCMEntities();

            Core.Data.ChargePoint dataPreviewPOI = new Core.Data.ChargePoint();
            PopulateChargePoint_SimpleToData(poi, dataPreviewPOI, dataModel);

            return Model.Extensions.ChargePoint.FromDataModel(dataPreviewPOI);
        }
        /// <summary>
        /// Check if user can edit given POI with review/approval from another editor
        /// </summary>
        /// <param name="poi"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool CanUserEditPOI(Model.ChargePoint poi, Model.User user)
        {
            if (user == null || poi == null) return false;

            int? countryId = (poi.AddressInfo != null && poi.AddressInfo.Country != null) ? (int?)poi.AddressInfo.Country.ID : null;

            if (UserManager.IsUserAdministrator(user) || UserManager.HasUserPermission(user, StandardPermissionAttributes.CountryLevel_Editor, "All") || (countryId != null && UserManager.HasUserPermission(user, StandardPermissionAttributes.CountryLevel_Editor, countryId.ToString())))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        [DbFunctionAttribute("OCM.Core.Data.OCMEntities.Store", "udf_GetDistanceFromLatLonKM")]
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
        public List<Model.ChargePoint> GetChargePoints(APIRequestSettings settings)
        {

            var stopwatch = new Stopwatch();
            stopwatch.Start();

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
                var dataModel = new OCMEntities();
                dataModel.Configuration.LazyLoadingEnabled = true;
                dataModel.Configuration.AutoDetectChangesEnabled = false;
                ((IObjectContextAdapter)dataModel).ObjectContext.CommandTimeout = 180; //allow longer time for query to complete

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

                //either filter by named country code or by country id list
                if (settings.CountryCode != null)
                {
                    var filterCountry = dataModel.Countries.FirstOrDefault(c => c.ISOCode == settings.CountryCode);
                    if (filterCountry != null)
                    {
                        filterByCountries = true;
                        settings.CountryIDs = new int[] { filterCountry.ID };
                    }
                    else
                    {
                        filterByCountries = false;
                        settings.CountryIDs = new int[] { -1 };
                    }
                }
                else
                {
                    if (settings.CountryIDs != null) { filterByCountries = true; }
                    else { settings.CountryIDs = new int[] { -1 }; }
                }

                if (settings.UsageTypeIDs != null) { filterByUsage = true; }
                else { settings.UsageTypeIDs = new int[] { -1 }; }

                if (settings.StatusTypeIDs != null) { filterByStatus = true; }
                else { settings.StatusTypeIDs = new int[] { -1 }; }

                if (settings.DataProviderIDs != null) { filterByDataProvider = true; }
                else { settings.DataProviderIDs = new int[] { -1 }; }

                if (settings.SubmissionStatusTypeID == -1) settings.SubmissionStatusTypeID = null;

                //compile initial list of locations
                var chargePointList = from c in dataModel.ChargePoints
                                        
                                      where
                                          //c.ParentChargePointID == null //exclude under review and delisted charge points
                                          (c.AddressInfo != null && c.AddressInfo.Latitude != null && c.AddressInfo.Longitude != null && c.AddressInfo.CountryID != null)
                                          && ((settings.SubmissionStatusTypeID == null && (c.SubmissionStatusTypeID == null || c.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Imported_Published || c.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Submitted_Published))
                                                || (settings.SubmissionStatusTypeID == 0) //return all regardless of status
                                                || (settings.SubmissionStatusTypeID != null && c.SubmissionStatusTypeID == settings.SubmissionStatusTypeID)
                                                ) //by default return live cps only, otherwise use specific submission statusid
                                          && (c.SubmissionStatusTypeID != (int)StandardSubmissionStatusTypes.Delisted_NotPublicInformation)
                                          && (settings.ChargePointID == null || c.ID == settings.ChargePointID)
                                          && (settings.OperatorName == null || c.Operator.Title == settings.OperatorName)
                                          && (settings.DataProviderName == null || c.DataProvider.Title == settings.DataProviderName)
                                          && (settings.LocationTitle == null || c.AddressInfo.Title.Contains(settings.LocationTitle))
                                          && (settings.ConnectionType == null || c.Connections.Any(conn => conn.ConnectionType.Title == settings.ConnectionType))
                                          && (settings.MinPowerKW == null || c.Connections.Any(conn => conn.PowerKW >= settings.MinPowerKW))
                                          && (filterByCountries == false || (filterByCountries == true && settings.CountryIDs.Contains((int)c.AddressInfo.CountryID)))
                                          && (filterByConnectionTypes == false || (filterByConnectionTypes == true && c.Connections.Any(conn => settings.ConnectionTypeIDs.Contains(conn.ConnectionType.ID))))
                                          && (filterByLevels == false || (filterByLevels == true && c.Connections.Any(chg => settings.LevelIDs.Contains((int)chg.ChargerType.ID))))
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


                //query is of type IQueryable
#if DEBUG
                string sql = filteredList.ToString();

                //writes to output window
                System.Diagnostics.Debug.WriteLine(sql);
#endif
                var additionalFilteredList = filteredList.Take(maxResults).ToList();

                stopwatch.Stop();

                System.Diagnostics.Debug.WriteLine("Total query time: " + stopwatch.Elapsed.ToString());

                stopwatch.Restart();

                foreach (var item in additionalFilteredList) //.ToList
                {
                    //note: if include comments is enabled, media items and metadata values are also included
                    Model.ChargePoint c = Model.Extensions.ChargePoint.FromDataModel(item.c, settings.IncludeComments, settings.IncludeComments, settings.IncludeComments, !settings.IsCompactOutput);

                    if (requiresDistance && c.AddressInfo != null)
                    {
                        c.AddressInfo.Distance = item.DistanceKM;
                        if (settings.DistanceUnit == Model.DistanceUnit.Miles && c.AddressInfo.Distance != null) c.AddressInfo.Distance = GeoManager.ConvertKMToMiles(c.AddressInfo.Distance);
                        c.AddressInfo.DistanceUnit = settings.DistanceUnit;
                    }

                    if (settings.IsLegacyAPICall && !(settings.APIVersion >= 2))
                    {
                        //for legacy callers, produce artificial list of Charger items
#pragma warning disable 612  //suppress obsolete warning
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

#pragma warning restore 612

                    if (c != null)
                    {
                        dataList.Add(c);
                    }
                }

                //cache results (if caching enabled)
                if (settings.EnableCaching == true)
                {
                    HttpContext.Current.Cache.Add(cacheKey, dataList, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 30, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
                }

            }
            else
            {
                dataList = (List<Model.ChargePoint>)HttpContext.Current.Cache[cacheKey];
            }

            System.Diagnostics.Debug.WriteLine("POI List Conversion to simple data model: " + stopwatch.Elapsed.ToString());


            return dataList.Take(settings.MaxResults).ToList();
        }

        /// <summary>
        /// for given charge point, return list of similar charge points based on location/title etc with approx similarity
        /// </summary>
        /// <param name="poi"></param>
        /// <returns></returns>
        public List<Model.ChargePoint> FindSimilar(Model.ChargePoint poi)
        {
            List<Model.ChargePoint> list = new List<Model.ChargePoint>();

            OCMEntities dataModel = new OCMEntities();

            //find similar locations (excluding same cp)
            var similarData = dataModel.ChargePoints.Where(c =>
                        c.ID != poi.ID
                        && c.ParentChargePointID == null //exclude under review and delisted charge points
                        && (c.SubmissionStatusTypeID == null || c.SubmissionStatusTypeID == 100 || c.SubmissionStatusTypeID == 200)
                        && (
                            c.AddressInfoID == poi.AddressInfo.ID
                            ||
                            c.AddressInfo.Postcode == poi.AddressInfo.Postcode
                            ||
                            c.AddressInfo.AddressLine1 == poi.AddressInfo.AddressLine1
                             ||
                            c.AddressInfo.Title == poi.AddressInfo.Title
                            )
                        );

            foreach (var item in similarData)
            {
                Model.ChargePoint c = Model.Extensions.ChargePoint.FromDataModel(item);

                if (c != null)
                {
                    int percentageSimilarity = 0;
                    if (c.AddressInfo.ID == poi.AddressInfo.ID) percentageSimilarity += 75;
                    if (c.AddressInfo.Postcode == poi.AddressInfo.Postcode) percentageSimilarity += 20;
                    if (c.AddressInfo.AddressLine1 == poi.AddressInfo.AddressLine1) percentageSimilarity += 50;
                    if (c.AddressInfo.Title == poi.AddressInfo.Title) percentageSimilarity += 25;
                    if (percentageSimilarity > 100) percentageSimilarity = 99;
                    c.PercentageSimilarity = percentageSimilarity;
                    list.Add(c);
                }
            }
            return list;
        }

        public POIDuplicates GetAllPOIDuplicates(int countryId)
        {
            List<DuplicatePOIItem> allDuplicates = new List<DuplicatePOIItem>();
            
            OCMEntities dataModel = new OCMEntities();

            double DUPLICATE_DISTANCE_METERS = 25;
            double POSSIBLE_DUPLICATE_DISTANCE_METERS = 50;

            //TODO: better method for large number of POIs
            //grab all live POIs (30-100,000 items)
            var allPOIs = dataModel.ChargePoints.Where(s=>s.AddressInfo.CountryID==countryId && (s.SubmissionStatusTypeID==100 || s.SubmissionStatusTypeID==200)).ToList();
            foreach (var poi in allPOIs)
            {
                //find pois which duplicate the given poi
                var dupePOIs = allPOIs.Where(p =>p.ID!=poi.ID &&
                    (
                        p.DataProvidersReference != null && p.DataProvidersReference.Length > 0 && p.DataProvidersReference == poi.DataProvidersReference
                        || new System.Device.Location.GeoCoordinate(p.AddressInfo.Latitude, p.AddressInfo.Longitude).GetDistanceTo(new System.Device.Location.GeoCoordinate(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude)) < POSSIBLE_DUPLICATE_DISTANCE_METERS
                    )
                    );


                    var poiModel = OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(poi);

                    foreach(var dupe in dupePOIs)
                    {
                        //poi has duplicates
                        DuplicatePOIItem dupePoi = new DuplicatePOIItem { DuplicatePOI = OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(dupe), DuplicateOfPOI = poiModel};
                        dupePoi.Reasons = new List<string>();
                        if (dupe.AddressInfo.Latitude == poi.AddressInfo.Latitude && dupe.AddressInfo.Longitude == poi.AddressInfo.Longitude)
                        {
                            dupePoi.Reasons.Add("POI location is exact match for OCM-"+poi.ID);
                            dupePoi.Confidence=95;
                        }
                        else
                        {
                            double distanceMeters =  new System.Device.Location.GeoCoordinate(dupe.AddressInfo.Latitude, dupe.AddressInfo.Longitude).GetDistanceTo(new System.Device.Location.GeoCoordinate(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude));
                            if (distanceMeters<DUPLICATE_DISTANCE_METERS)
                            {
                                dupePoi.Reasons.Add("POI location is close proximity ("+distanceMeters+"m) to OCM-"+poi.ID);
                                dupePoi.Confidence=75;
                            } else {
                                if (distanceMeters<POSSIBLE_DUPLICATE_DISTANCE_METERS){
                                    dupePoi.Reasons.Add("POI location is in surrounding proximity ("+distanceMeters+"m) to OCM-"+poi.ID);
                                    dupePoi.Confidence=50;
                                }
                            }
                        }

                        allDuplicates.Add(dupePoi);
                    }
            }

            //arrange all duplicates into groups
            POIDuplicates duplicatesSummary = new POIDuplicates();
            duplicatesSummary.DuplicateSummaryList = new List<DuplicatePOIGroup>();
            foreach (var dupe in allDuplicates)
            {
                bool isNewGroup = false;
                var dupeGroup = duplicatesSummary.DuplicateSummaryList.FirstOrDefault(d=>d.DuplicatePOIList.Any(p=>p.DuplicateOfPOI.ID== dupe.DuplicateOfPOI.ID || p.DuplicatePOI.ID==dupe.DuplicatePOI.ID) || d.SuggestedBestPOI.ID==dupe.DuplicatePOI.ID);
                if (dupeGroup == null)
                {
                    isNewGroup = true;
                    dupeGroup = new DuplicatePOIGroup();
                    dupeGroup.SuggestedBestPOI = dupe.DuplicatePOI;//TODO: select best
                    
                    dupeGroup.DuplicatePOIList = new List<DuplicatePOIItem>();
                }

                //only add to dupe group if not already added for another reason
                if (!dupeGroup.DuplicatePOIList.Contains(dupe) && !dupeGroup.DuplicatePOIList.Any(d=>d.DuplicatePOI.ID==dupe.DuplicatePOI.ID))
                {
                    dupeGroup.DuplicatePOIList.Add(dupe);
                }

                if (isNewGroup)
                {
                    duplicatesSummary.DuplicateSummaryList.Add(dupeGroup);
                }
            }

            //TODO: go through all dupe groups and nominate best poi to be main poi (most comments, most equipment info etc)
            return duplicatesSummary;
        }

        public static bool IsValid(Model.ChargePoint cp)
        {
            //determine if the basic CP details are valid as a submission or edit
            if (cp.AddressInfo == null) return false;
            if (String.IsNullOrEmpty(cp.AddressInfo.Title)) return false;

            if (cp.AddressInfo.Country == null) return false;

            if (cp.AddressInfo.Latitude == 0 && cp.AddressInfo.Longitude == 0) return false;

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

            var objectComparison = new CompareLogic(new ComparisonConfig { CompareChildren=true, MaxDifferences=1000});
         
            var exclusionList = new string[]
                {
                    "DateCreated",
                    "DateLastStatusUpdate",
                    ".DataProvider.WebsiteURL",
                    ".DataProvider.DataProviderStatusType",
                    ".StatusType.IsOperational",
                    ".ConnectionType.FormalName",
                    ".Level.Comments",
                    ".AddressInfo.Distance",
                    ".AddressInfo.DistanceUnit",
                    ".AddressInfo.DistanceUnit.DistanceUnit",
                    ".SubmissionStatus.IsLive",
                    ".OperatorInfo.WebsiteURL",
                    ".OperatorInfo.PhonePrimaryContact",
                    ".OperatorInfo.IsPrivateIndividual",
                    ".OperatorInfo.ContactEmail",
                    ".OperatorInfo.FaultReportEmail",
                    ".UsageType.IsPayAtLocation",
                    ".UsageType.IsMembershipRequired",
                    ".UsageType.IsAccessKeyRequired",
                    ".UsageType.ID",
                    ".DataProvider.ID",
                    ".DataProvider.DataProviderStatusType.ID",
                    ".DataProvider.DataProviderStatusType.Title"
                };
            objectComparison.Config.MembersToIgnore.AddRange(exclusionList);

            var comparisonResult = objectComparison.Compare(poiA, poiB);

            if (!comparisonResult.AreEqual)
            {
                //clean up differences we want to exclude
                foreach (var exclusionSuffix in exclusionList)
                {
                    comparisonResult.Differences.RemoveAll(e => e.PropertyName.EndsWith(exclusionSuffix));
                }

                diffList.AddRange(comparisonResult.Differences.Select(difference => new DiffItem { Context = difference.PropertyName, ValueA = difference.Object1Value, ValueB = difference.Object2Value }));

                //remove items which only vary on null vs ""
                diffList.RemoveAll(d => (String.IsNullOrWhiteSpace(d.ValueA) || d.ValueA == "(null)") && (String.IsNullOrWhiteSpace(d.ValueB) || d.ValueB == "(null)"));

                //remove items which are in fact the same
                diffList.RemoveAll(d => d.ValueA == d.ValueB);
            }
            return diffList;
        }


        /// <summary>
        /// Populate AddressInfo data from settings in a simple AddressInfo object
        /// </summary>
        public Core.Data.AddressInfo PopulateAddressInfo_SimpleToData(Model.AddressInfo simpleAddressInfo, Core.Data.AddressInfo dataAddressInfo, OCMEntities dataModel)
        {
            if (simpleAddressInfo != null && dataAddressInfo == null) dataAddressInfo = new Core.Data.AddressInfo();

            if (simpleAddressInfo != null && dataAddressInfo != null)
            {
                dataAddressInfo.Title = simpleAddressInfo.Title;
                dataAddressInfo.AddressLine1 = simpleAddressInfo.AddressLine1;
                dataAddressInfo.AddressLine2 = simpleAddressInfo.AddressLine2;
                dataAddressInfo.Town = simpleAddressInfo.Town;
                dataAddressInfo.StateOrProvince = simpleAddressInfo.StateOrProvince;
                dataAddressInfo.Postcode = simpleAddressInfo.Postcode;
                if (simpleAddressInfo.Country != null && simpleAddressInfo.Country.ID > 0)
                {
                    dataAddressInfo.Country = dataModel.Countries.FirstOrDefault(c => c.ID == simpleAddressInfo.Country.ID);
                    dataAddressInfo.CountryID = dataAddressInfo.Country.ID;
                }
                dataAddressInfo.Latitude = simpleAddressInfo.Latitude;
                dataAddressInfo.Longitude = simpleAddressInfo.Longitude;
                dataAddressInfo.ContactTelephone1 = simpleAddressInfo.ContactTelephone1;
                dataAddressInfo.ContactTelephone2 = simpleAddressInfo.ContactTelephone2;
                dataAddressInfo.ContactEmail = simpleAddressInfo.ContactEmail;
                dataAddressInfo.AccessComments = simpleAddressInfo.AccessComments;
#pragma warning disable 612 //suppress obsolete warning
                dataAddressInfo.GeneralComments = simpleAddressInfo.GeneralComments;
#pragma warning restore 612 //suppress obsolete warning
                dataAddressInfo.RelatedURL = simpleAddressInfo.RelatedURL;
            }

            return dataAddressInfo;
        }

        public void PopulateChargePoint_SimpleToData(Model.ChargePoint simpleChargePoint, Core.Data.ChargePoint dataChargePoint, OCM.Core.Data.OCMEntities dataModel)
        {
            if (simpleChargePoint.ID > 0) dataChargePoint.ID = simpleChargePoint.ID; // dataModel.ChargePoints.FirstOrDefault(c => c.ID == simpleChargePoint.ID);

            if (String.IsNullOrEmpty(dataChargePoint.UUID)) dataChargePoint.UUID = Guid.NewGuid().ToString().ToUpper();

            //if required, set the parent charge point id
            if (simpleChargePoint.ParentChargePointID != null)
            {
                //dataChargePoint.ParentChargePoint = dataModel.ChargePoints.FirstOrDefault(c=>c.ID==simpleChargePoint.ParentChargePointID);
                dataChargePoint.ParentChargePointID = simpleChargePoint.ParentChargePointID;
            }
            else
            {
                dataChargePoint.ParentChargePoint = null;
                dataChargePoint.ParentChargePointID = null;
            }

            if (simpleChargePoint.DataProvider != null && simpleChargePoint.DataProvider.ID >= 0)
            {
                dataChargePoint.DataProvider = dataModel.DataProviders.First(d => d.ID == simpleChargePoint.DataProvider.ID);
            }
            else
            {
                //set to ocm contributor by default
                dataChargePoint.DataProvider = dataModel.DataProviders.First(d => d.ID == (int)StandardDataProviders.OpenChargeMapContrib);
            }

            dataChargePoint.DataProvidersReference = simpleChargePoint.DataProvidersReference;

            if (simpleChargePoint.OperatorInfo != null && simpleChargePoint.OperatorInfo.ID >= 0)
            {
                dataChargePoint.Operator = dataModel.Operators.First(o => o.ID == simpleChargePoint.OperatorInfo.ID);
            }
            dataChargePoint.OperatorsReference = simpleChargePoint.OperatorsReference;

            if (simpleChargePoint.UsageType != null && simpleChargePoint.UsageType.ID >= 0) dataChargePoint.UsageType = dataModel.UsageTypes.First(u => u.ID == simpleChargePoint.UsageType.ID);

            dataChargePoint.AddressInfo = PopulateAddressInfo_SimpleToData(simpleChargePoint.AddressInfo, dataChargePoint.AddressInfo, dataModel);

            dataChargePoint.NumberOfPoints = simpleChargePoint.NumberOfPoints;
            dataChargePoint.GeneralComments = simpleChargePoint.GeneralComments;
            dataChargePoint.DatePlanned = simpleChargePoint.DatePlanned;
            dataChargePoint.UsageCost = simpleChargePoint.UsageCost;

            if (simpleChargePoint.DateLastStatusUpdate != null)
            {
                dataChargePoint.DateLastStatusUpdate = DateTime.UtcNow;
            }
            else
            {
                dataChargePoint.DateLastStatusUpdate = DateTime.UtcNow;
            }


            if (simpleChargePoint.DataQualityLevel != null && (simpleChargePoint.DataQualityLevel >= 0 && simpleChargePoint.DataQualityLevel <= 5))
            {
                dataChargePoint.DataQualityLevel = simpleChargePoint.DataQualityLevel;
            }
            else
            {
                dataChargePoint.DataQualityLevel = 1;
            }

            if (simpleChargePoint.DateCreated != null)
            {
                dataChargePoint.DateCreated = simpleChargePoint.DateCreated;
            }
            else
            {
                if (dataChargePoint.DateCreated == null) dataChargePoint.DateCreated = DateTime.UtcNow;
            }

            var updateConnectionList = new List<OCM.Core.Data.ConnectionInfo>();
            var deleteList = new List<OCM.Core.Data.ConnectionInfo>();

            if (simpleChargePoint.Connections != null)
            {
                foreach (var c in simpleChargePoint.Connections)
                {
                    var connectionInfo = new Core.Data.ConnectionInfo();

                    //edit existing, if required
                    if (c.ID > 0) connectionInfo = dataChargePoint.Connections.FirstOrDefault(con => con.ID == c.ID && con.ChargePointID == dataChargePoint.ID);
                    if (connectionInfo == null)
                    {
                        //connection is stale info, start new
                        c.ID = 0;
                        connectionInfo = new Core.Data.ConnectionInfo();
                    }

                    connectionInfo.Reference = c.Reference;
                    connectionInfo.Comments = c.Comments;
                    connectionInfo.Amps = c.Amps;
                    connectionInfo.Voltage = c.Voltage;
                    connectionInfo.Quantity = c.Quantity;
                    connectionInfo.PowerKW = c.PowerKW;

                    if (c.ConnectionType != null && c.ConnectionType.ID >= 0)
                    {
                        connectionInfo.ConnectionType = dataModel.ConnectionTypes.First(ct => ct.ID == c.ConnectionType.ID);
                    }
                    else
                    {
                        connectionInfo.ConnectionType = null;
                    }

                    if (c.Level != null && c.Level.ID >= 1)
                    {
                        connectionInfo.ChargerType = dataModel.ChargerTypes.First(chg => chg.ID == c.Level.ID);
                    }
                    else
                    {
                        connectionInfo.ChargerType = null;
                    }

                    if (c.CurrentType != null && c.CurrentType.ID >= 10)
                    {
                        connectionInfo.CurrentType = dataModel.CurrentTypes.First(chg => chg.ID == c.CurrentType.ID);
                    }
                    else
                    {
                        connectionInfo.CurrentType = null;
                    }

                    if (c.StatusType != null && c.StatusType.ID >= 0)
                    {
                        connectionInfo.StatusType = dataModel.StatusTypes.First(s => s.ID == c.StatusType.ID);
                    }
                    else
                    {
                        connectionInfo.StatusType = null;
                    }

                    bool addConnection = false;

                    //detect if connection details are non-blank/unknown before adding
                    if (
                        !String.IsNullOrEmpty(connectionInfo.Reference)
                        || !String.IsNullOrEmpty(connectionInfo.Comments)
                        || connectionInfo.Amps != null
                        || connectionInfo.Voltage != null
                        || connectionInfo.PowerKW != null
                        || (connectionInfo.ConnectionType != null && connectionInfo.ConnectionType.ID > 0)
                        || (connectionInfo.StatusType != null && connectionInfo.StatusTypeID > 0)
                        || (connectionInfo.ChargerType != null && connectionInfo.ChargerType.ID > 1)
                        || (connectionInfo.CurrentType != null && connectionInfo.CurrentType.ID > 0)
                        || (connectionInfo.Quantity != null && connectionInfo.Quantity > 1)
                    )
                    {
                        addConnection = true;
                        if (connectionInfo.ChargePoint == null) connectionInfo.ChargePoint = dataChargePoint;
                    }

                    if (addConnection)
                    {
                        //if adding a new connection (not an update) add to model
                        if (c.ID <= 0)
                        {
                            dataChargePoint.Connections.Add(connectionInfo);
                        }
                        //track final list of connections being added/updated  -- will then be used to delete by difference
                        updateConnectionList.Add(connectionInfo);
                    }
                    else
                    {
                        //remove an existing connection no longer required
                        //if (c.ID > 0)
                        {
                            if (connectionInfo.ChargePoint == null) connectionInfo.ChargePoint = dataChargePoint;
                            deleteList.Add(connectionInfo);
                            //dataChargePoint.Connections.Remove(connectionInfo);
                        }
                    }
                }
            }

            //find existing connections not in updated/added list, add to delete
            if (dataChargePoint.Connections != null)
            {
                foreach (var con in dataChargePoint.Connections)
                {
                    if (!updateConnectionList.Contains(con))
                    {
                        if (!deleteList.Contains(con))
                        {
                            deleteList.Add(con);
                        }
                    }
                }
            }

            //finally clean up deleted items
            foreach (var item in deleteList)
            {
                if (item.ID > 0)
                {
                    dataModel.ConnectionInfoList.Remove(item);
                }
            }

            if (dataChargePoint.MetadataValues == null)
            {
                dataChargePoint.MetadataValues = new List<OCM.Core.Data.MetadataValue>();
                //add metadata values
            }

            if (simpleChargePoint.MetadataValues != null)
            {
                foreach (var m in simpleChargePoint.MetadataValues)
                {
                    var existingValue = dataChargePoint.MetadataValues.FirstOrDefault(v => v.ID == m.ID);
                    if (existingValue != null)
                    {
                        existingValue.ItemValue = m.ItemValue;
                    }
                    else
                    {
                        var newValue = new OCM.Core.Data.MetadataValue()
                        {
                            ChargePointID = dataChargePoint.ID,
                            ItemValue = m.ItemValue,
                            MetadataFieldID = m.MetadataFieldID,
                            MetadataField = dataModel.MetadataFields.FirstOrDefault(f => f.ID == m.MetadataFieldID)
                        };
                        dataChargePoint.MetadataValues.Add(newValue);
                    }
                }
            }

            //TODO:clean up unused metadata values



            if (simpleChargePoint.StatusTypeID != null || simpleChargePoint.StatusType != null)
            {
                if (simpleChargePoint.StatusTypeID == null && simpleChargePoint.StatusType != null) simpleChargePoint.StatusTypeID = simpleChargePoint.StatusType.ID;
                dataChargePoint.StatusTypeID = simpleChargePoint.StatusTypeID;
                dataChargePoint.StatusType = dataModel.StatusTypes.FirstOrDefault(s => s.ID == simpleChargePoint.StatusTypeID);
            }
            else
            {
                dataChargePoint.StatusType = null;
                dataChargePoint.StatusTypeID = null;
            }

            if (simpleChargePoint.SubmissionStatusTypeID != null || simpleChargePoint.SubmissionStatus != null)
            {
                if (simpleChargePoint.SubmissionStatusTypeID == null & simpleChargePoint.SubmissionStatus != null) simpleChargePoint.SubmissionStatusTypeID = simpleChargePoint.SubmissionStatus.ID;
                dataChargePoint.SubmissionStatusType = dataModel.SubmissionStatusTypes.First(s => s.ID == simpleChargePoint.SubmissionStatusTypeID);
                dataChargePoint.SubmissionStatusTypeID = simpleChargePoint.SubmissionStatusTypeID;
            }
            else
            {
                dataChargePoint.SubmissionStatusTypeID = null;
                dataChargePoint.SubmissionStatusType = null; // dataModel.SubmissionStatusTypes.First(s => s.ID == (int)StandardSubmissionStatusTypes.Submitted_UnderReview);
            }

        }

        /// <summary>
        /// used to replace a POI from an external source with a new OCM provided POI, updated POI must still be saved after to store association
        /// </summary>
        /// <param name="oldPOI"></param>
        /// <param name="updatedPOI"></param>
        public int SupersedePOI(OCMEntities dataModel, Model.ChargePoint oldPOI, Model.ChargePoint updatedPOI)
        {
            //When applying an edit to imported or externally provided data:
            //Save old item with new POI ID, Submission Status: Delisted - Superseded By Edit
            //Save edit over old POI ID, with Contributor set to OCM, Parent POI ID set to new (old) POI ID
            //This means that comments/photos/metadata etc for the old POI are preserved against the new POI

            //zero existing ids we want to move to superseded poi (otherwise existing items will be updated)
            oldPOI.ID = 0;
            oldPOI.UUID = null;
            if (oldPOI.AddressInfo != null) oldPOI.AddressInfo.ID = 0;
            if (oldPOI.Connections != null)
            {
                foreach (var connection in oldPOI.Connections)
                {
                    connection.ID = 0;
                }
            }
            oldPOI.SubmissionStatus = null;
            oldPOI.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Delisted_SupersededByUpdate;

            var supersededPOIData = new OCM.Core.Data.ChargePoint();
            this.PopulateChargePoint_SimpleToData(oldPOI, supersededPOIData, dataModel);
            dataModel.ChargePoints.Add(supersededPOIData);
            dataModel.SaveChanges();

            //associate updated poi with older parent poi, set OCM as new data provider
            updatedPOI.ParentChargePointID = supersededPOIData.ID;
            updatedPOI.DataProvider = null;
            updatedPOI.DataProviderID = (int)StandardDataProviders.OpenChargeMapContrib;

            //return new ID for the archived version of the POI
            return supersededPOIData.ID;
        }
    }

}