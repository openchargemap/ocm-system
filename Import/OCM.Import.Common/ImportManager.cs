using i4o.i4o;
using KellermanSoftware.CompareNetObjects;
using Newtonsoft.Json;
using OCM.API.Client;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.Import.Misc;
using OCM.Import.Providers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.Import
{
    /*Import Reporting:
     * before performing import, prepare report of items to be added/updated, ignored or delisted.
     * for each item, detail the reason for the categorisation and related items from the import
     * */

    public enum ImportItemStatus
    {
        Added,
        Updated,
        Ignored,
        Delisted,
        IsDuplicate,
        LowQuality,
        MergeSource
    }

    public class ImportItem
    {
        public ImportItemStatus Status { get; set; }

        public string Comment { get; set; }

        /// <summary>
        /// If true, item is from the data source, otherwise item is target in OCM to be added/updated.
        /// </summary>
        public bool IsSourceItem { get; set; }

        public ChargePoint POI { get; set; }

        public List<ImportItem> RelatedItems { get; set; }

        public ImportItem()
        {
            RelatedItems = new List<ImportItem>();
        }
    }

    public class ImportReport
    {
        public BaseImportProvider ProviderDetails { get; set; }

        public List<ImportItem> ImportItems { get; set; }

        public List<ChargePoint> Added { get; set; }

        public List<ChargePoint> Updated { get; set; }

        public List<ChargePoint> Unchanged { get; set; }

        public List<ChargePoint> LowDataQuality { get; set; }

        public List<ChargePoint> Delisted { get; set; }

        public List<ChargePoint> Duplicates { get; set; }

        public string Log { get; set; }

        public ImportReport()
        {
            ImportItems = new List<ImportItem>();

            Added = new List<ChargePoint>();
            Updated = new List<ChargePoint>();
            Unchanged = new List<ChargePoint>();
            LowDataQuality = new List<ChargePoint>();
            Delisted = new List<ChargePoint>();
            Duplicates = new List<ChargePoint>();
        }
    }

    public class ImportManager
    {
        public const int DUPLICATE_DISTANCE_METERS = 200;

        public bool ImportUpdatesOnly { get; set; }

        public bool IsSandboxedAPIMode { get; set; }

        public string GeonamesAPIUserName { get; set; }

        public string TempFolder { get; set; }

        private GeolocationCacheManager geolocationCacheManager = null;

        public string ImportLog { get; set; }

        public ImportManager(string tempFolderPath)
        {
            GeonamesAPIUserName = "openchargemap";
            TempFolder = tempFolderPath;
            geolocationCacheManager = new GeolocationCacheManager(TempFolder);
            geolocationCacheManager.GeonamesAPIUserName = GeonamesAPIUserName;
            geolocationCacheManager.LoadCache();
        }

        /**
         * Generic Import Process

            Provider Properties
                Import Method
                Import URL/Path
                Import Frequency
                IsMaster

            Fetch Latest Data

            For each item
                Check If Exists or Strong Duplicate, Get ID
                If New, Add
                if Exists Then
                    Prepare update, if provider supports live status, set that
                        What if item updated manually on OCM?
                    Send Update
                End
            Loop

            Log Exceptions
            Log Count of Items Added or Modified

            Way to remove item (or log items) which no longer exist in data source?
         * */

        public async Task<List<ChargePoint>> DeDuplicateList(List<ChargePoint> cpList, bool updateDuplicate, CoreReferenceData coreRefData, ImportReport report, bool allowDupeWithDifferentOperator = false)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            //get list of all current POIs (in relevant countries) including most delisted ones
            int[] countryIds = (from poi in cpList
                                where poi.AddressInfo.Country != null
                                select poi.AddressInfo.Country.ID).Distinct().ToArray();

            APIRequestParams filters = new APIRequestParams { CountryIDs = countryIds, MaxResults = 1000000, EnableCaching = false, SubmissionStatusTypeID = 0 };
            //List<ChargePoint> masterList = await new OCMClient(IsSandboxedAPIMode).GetLocations(filters); //new OCMClient().FindSimilar(null, 10000); //fetch all charge points regardless of status
            var poiManager = new POIManager();

            List<ChargePoint> masterListCollection = poiManager.GetChargePoints(filters); //new OCMClient().FindSimilar(null, 10000); //fetch all charge points regardless of status

            var spec = new i4o.IndexSpecification<ChargePoint>()
                    .Add(i => i.DataProviderID)
                    .Add(i => i.DataProvidersReference)
                    ;

            var masterList = new i4o.IndexSet<ChargePoint>(masterListCollection, spec);

            List<ChargePoint> masterListCopy = new List<ChargePoint>();
            foreach (var tmp in masterList)
            {
                //fully copy of master list item so we have before/after
                masterListCopy.Add(JsonConvert.DeserializeObject<ChargePoint>(JsonConvert.SerializeObject(tmp)));
            }

            //if we failed to get a master list, quit with no result
            if (masterListCollection.Count == 0) return new List<ChargePoint>();

            List<ChargePoint> duplicateList = new List<ChargePoint>();
            List<ChargePoint> updateList = new List<ChargePoint>();

            ChargePoint previousCP = null;

            //for each item to be imported, deduplicate by adding to updateList only the items which we don't already haves
            var cpListSortedByPos = cpList.OrderBy(c => c.AddressInfo.Latitude).ThenBy(c => c.AddressInfo.Longitude);

            int poiProcessed = 0;
            int totalPOI = cpListSortedByPos.Count();

            Stopwatch dupeIdentWatch = new Stopwatch();
            dupeIdentWatch.Start();

            foreach (var item in cpListSortedByPos)
            {
                var itemGeoPos = new System.Device.Location.GeoCoordinate(item.AddressInfo.Latitude, item.AddressInfo.Longitude);

                //item is duplicate if we already seem to have it based on Data Providers reference or approx position match
                var dupeList = masterList.Where(c =>
                        (c.DataProvider != null && c.DataProviderID == item.DataProviderID && c.DataProvidersReference == item.DataProvidersReference)
                        || (c.AddressInfo.Title == item.AddressInfo.Title && c.AddressInfo.AddressLine1 == item.AddressInfo.AddressLine1 && c.AddressInfo.Postcode == item.AddressInfo.Postcode)
                        || (GeoManager.IsClose(c.AddressInfo.Latitude, c.AddressInfo.Longitude, item.AddressInfo.Latitude, item.AddressInfo.Longitude) && new System.Device.Location.GeoCoordinate(c.AddressInfo.Latitude, c.AddressInfo.Longitude).GetDistanceTo(itemGeoPos) < DUPLICATE_DISTANCE_METERS) //meters distance apart
                );

                if (dupeList.Any())
                {
                    if (updateDuplicate)
                    {
                        //if updating duplicates, get exact matching duplicate based on provider reference and update/merge with this item to update status/merge properties
                        var updatedItem = dupeList.FirstOrDefault(d => d.DataProviderID == (item.DataProvider != null ? item.DataProvider.ID : item.DataProviderID) && d.DataProvidersReference == item.DataProvidersReference);
                        if (updatedItem != null)
                        {
                            //only merge/update from live published items
                            if (updatedItem.SubmissionStatus.IsLive == (bool?)true
                                || updatedItem.SubmissionStatus.ID == (int)StandardSubmissionStatusTypes.Delisted_RemovedByDataProvider
                                 || updatedItem.SubmissionStatus.ID == (int)StandardSubmissionStatusTypes.Delisted_NotPublicInformation)
                            {
                                //item is an exact match from same data provider
                                //overwrite existing with imported data (use import as master)
                                //updatedItem = poiManager.PreviewPopulatedPOIFromModel(updatedItem);
                                MergeItemChanges(item, updatedItem, false);

                                updateList.Add(updatedItem);
                            }
                        }

                        if (updatedItem == null)
                        {
                            //duplicates are not exact match
                            //TODO: resolve whether imported data should change duplicate

                            //merge new properties from imported item
                            //if (item.StatusType != null) updatedItem.StatusType = item.StatusType;
                            //updateList.Add(updatedItem);
                        }
                    }

                    //item has one or more likely duplicates, add it to list of items to remove
                    duplicateList.Add(item);
                }

                //mark item as duplicate if location/title exactly matches previous entry or lat/long is within DuplicateDistance meters

                if (previousCP != null)
                {
                    //this branch is the most expensive part of dedupe:
                    if (IsDuplicateLocation(item, previousCP, true))
                    {
                        if (!duplicateList.Contains(item))
                        {
                            if (allowDupeWithDifferentOperator && item.OperatorID != previousCP.OperatorID)
                            {
                                Log("Duplicated allowed due to different operator:" + item.AddressInfo.Title);
                            }
                            else
                            {
                                Log("Duplicated item removed:" + item.AddressInfo.Title);
                                duplicateList.Add(item);
                            }
                        }
                    }
                }

                previousCP = item;

                poiProcessed++;

                if (poiProcessed % 300 == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Deduplication: " + poiProcessed + " processed of " + totalPOI);
                }
            }

            dupeIdentWatch.Stop();
            Log("De-dupe pass took " + dupeIdentWatch.Elapsed.TotalSeconds + " seconds. " + (dupeIdentWatch.Elapsed.TotalMilliseconds / cpList.Count) + "ms per item.");

            //remove duplicates from list to apply
            foreach (var dupe in duplicateList)
            {
                cpList.Remove(dupe);
            }

            Log("Duplicates removed from import:" + duplicateList.Count);

            //add updated items (replace duplicates with property changes)

            foreach (var updatedItem in updateList)
            {
                if (!cpList.Contains(updatedItem))
                {
                    cpList.Add(updatedItem);
                }
            }

            Log("Updated items to import:" + updateList.Count);

            //populate missing location info from geolocation cache if possible
            Stopwatch geoWatch = new Stopwatch();
            geoWatch.Start();
            PopulateLocationFromGeolocationCache(cpList, coreRefData);
            geoWatch.Stop();
            Log("Populate Country from Lat/Long took " + geoWatch.Elapsed.TotalSeconds + " seconds. " + (geoWatch.Elapsed.TotalMilliseconds / cpList.Count) + "ms per item.");

            //final pass to catch duplicates present in data source, mark additional items as Delisted Duplicate so we have a record for them
            var submissionStatusDelistedDupe = coreRefData.SubmissionStatusTypes.First(s => s.ID == 1001); //delisted duplicate
            previousCP = null;

            //sort current cp list by position again
            cpListSortedByPos = cpList.OrderBy(c => c.AddressInfo.Latitude).ThenBy(c => c.AddressInfo.Longitude);

            //mark any duplicates in final list as delisted duplicates (submitted to api)
            foreach (var cp in cpListSortedByPos)
            {
                bool isDuplicate = false;
                if (previousCP != null)
                {
                    isDuplicate = IsDuplicateLocation(cp, previousCP, false);
                    if (isDuplicate)
                    {
                        cp.SubmissionStatus = submissionStatusDelistedDupe;
                        cp.SubmissionStatusTypeID = submissionStatusDelistedDupe.ID;
                        if (previousCP.ID > 0)
                        {
                            if (cp.GeneralComments == null) cp.GeneralComments = "";
                            cp.GeneralComments += " [Duplicate of OCM-" + previousCP.ID + "]";
                            cp.ParentChargePointID = previousCP.ID;
                        }
                    }
                }

                if (!isDuplicate)
                {
                    previousCP = cp;
                }
            }

            report.Added = cpListSortedByPos.Where(cp => cp.ID == 0).ToList();
            report.Updated = cpListSortedByPos.Where(cp => cp.ID > 0).ToList();
            report.Duplicates = duplicateList; //TODO: add additional pass of duplicates from above

            //determine which POIs in our master list are no longer referenced in the import
            report.Delisted = masterList.Where(cp => cp.DataProviderID == report.ProviderDetails.DataProviderID && cp.SubmissionStatus != null && (cp.SubmissionStatus.IsLive == true || cp.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Imported_UnderReview)
                && !cpListSortedByPos.Any(master => master.ID == cp.ID) && !report.Duplicates.Any(master => master.ID == cp.ID)
                && cp.UserComments == null && cp.MediaItems == null).ToList();
            //safety check to ensure we're not delisting items just because we have incomplete import data:
            if (cpList.Count < 50)// || (report.Delisted.Count > cpList.Count))
            {
                report.Delisted = new List<ChargePoint>();
            }

            //determine list of low quality POIs (incomplete address info etc)
            report.LowDataQuality = new List<ChargePoint>();
            report.LowDataQuality.AddRange(GetLowDataQualityPOIs(report.Added));
            report.LowDataQuality.AddRange(GetLowDataQualityPOIs(report.Updated));

            Log("Removing " + report.LowDataQuality.Count + " low quality POIs from added/updated");

            //remove references in added/updated to any low quality POIs
            foreach (var p in report.LowDataQuality)
            {
                report.Added.Remove(p);
            }
            foreach (var p in report.LowDataQuality)
            {
                report.Updated.Remove(p);
            }

            //remove updates which only change datelaststatusupdate
            var updatesToIgnore = new List<ChargePoint>();
            foreach (var poi in report.Updated)
            {
                var origPOI = masterListCopy.FirstOrDefault(p => p.ID == poi.ID);
                var updatedPOI = poiManager.PreviewPopulatedPOIFromModel(poi);
                var differences = poiManager.CheckDifferences(origPOI, updatedPOI);
                differences.RemoveAll(d => d.Context == ".MetadataValues");
                differences.RemoveAll(d => d.Context == ".DateLastStatusUpdate");
                differences.RemoveAll(d => d.Context == ".UUID");

                differences.RemoveAll(d => d.Context == ".DataProvider.DateLastImported");
                differences.RemoveAll(d => d.Context == ".IsRecentlyVerified");
                differences.RemoveAll(d => d.Context == ".DateLastVerified");
                differences.RemoveAll(d => d.Context == ".UserComments");
                differences.RemoveAll(d => d.Context == ".MediaItems");

                if (!differences.Any())
                {
                    updatesToIgnore.Add(poi);
                }
                else
                {
                    //differences exist
                    CompareLogic compareLogic = new CompareLogic();
                    compareLogic.Config.MaxDifferences = 100;
                    compareLogic.Config.IgnoreObjectTypes = false;
                    compareLogic.Config.IgnoreUnknownObjectTypes = true;
                    compareLogic.Config.CompareChildren = true;
                    ComparisonResult result = compareLogic.Compare(origPOI, updatedPOI);

                    var diffReport = new KellermanSoftware.CompareNetObjects.Reports.UserFriendlyReport();
                    result.Differences.RemoveAll(d => d.PropertyName == ".MetadataValues");
                    result.Differences.RemoveAll(d => d.PropertyName == ".DateLastStatusUpdate");
                    result.Differences.RemoveAll(d => d.PropertyName == ".UUID");
                    result.Differences.RemoveAll(d => d.PropertyName == ".DataProvider.DateLastImported");
                    result.Differences.RemoveAll(d => d.PropertyName == ".IsRecentlyVerified");
                    result.Differences.RemoveAll(d => d.PropertyName == ".DateLastVerified");
                    result.Differences.RemoveAll(d => d.PropertyName == ".UserComments");
                    result.Differences.RemoveAll(d => d.PropertyName == ".MediaItems");
                    System.Diagnostics.Debug.WriteLine("Difference:" + diffReport.OutputString(result.Differences));

                    if (!result.Differences.Any())
                    {
                        updatesToIgnore.Add(poi);
                    }
                }
            }

            foreach (var p in updatesToIgnore)
            {
                if (report.Unchanged == null) report.Unchanged = new List<ChargePoint>();
                report.Unchanged.Add(p);
                report.Updated.Remove(p);
            }

            //TODO: if POi is a duplicate ensure imported data provider reference/URL  is included as reference metadata in OCM's version of the POI

            stopWatch.Stop();
            Log("Deduplicate List took " + stopWatch.Elapsed.TotalSeconds + " seconds");

            //return final processed list ready for applying as insert/updates
            return cpListSortedByPos.ToList();
        }

        public List<ChargePoint> MergeDuplicatePOIEquipment(List<ChargePoint> importedPOIList)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            List<ChargePoint> mergedPOIList = new List<ChargePoint>();

            List<ChargePoint> tmpMergedPOIs = new List<ChargePoint>();
            foreach (var poi in importedPOIList)
            {
                if (!tmpMergedPOIs.Contains(poi))
                {
                    int matchCount = 0;
                    foreach (var otherPoi in importedPOIList.Where(p => p != poi))
                    {
                        //if address matches or if address is empty and lat/long matches then merge the poi
                        if ((!String.IsNullOrEmpty(poi.AddressInfo.ToString()) && poi.AddressInfo.ToString() == otherPoi.AddressInfo.ToString()) || (String.IsNullOrEmpty(poi.AddressInfo.ToString()) && poi.AddressInfo.Latitude == otherPoi.AddressInfo.Latitude && poi.AddressInfo.Longitude == otherPoi.AddressInfo.Longitude))
                        {
                            if (poi.OperatorID != otherPoi.OperatorID)
                            {
                                Log("Merged POI has different Operator: " + poi.AddressInfo.ToString());
                            }
                            else
                            {
                                matchCount++;
                                tmpMergedPOIs.Add(otherPoi); //track which POIs will now be discarded

                                //add equipment info from merged poi to our main poi
                                if (otherPoi.Connections != null)
                                {
                                    if (poi.Connections == null) poi.Connections = new List<ConnectionInfo>();
                                    poi.Connections.AddRange(otherPoi.Connections);
                                }
                            }
                        }
                    }

                    if (matchCount > 0)
                    {
                        Log("POI equipment merged from " + matchCount + " other POIs. " + poi.AddressInfo.ToString());
                    }
                }

                mergedPOIList.Add(poi);
            }

            stopWatch.Stop();
            Log("MergeDuplicatePOIEquipmentList took " + stopWatch.Elapsed.TotalSeconds + " seconds");
            return mergedPOIList;
        }

        private void Log(string message)
        {
            this.ImportLog += message + "\r\n";
        }

        /// <summary>
        /// Given a list of POIs, returns list of those which are low data quality based on their content (address etc)
        /// </summary>
        /// <param name="allPOIs"></param>
        /// <returns></returns>
        public List<ChargePoint> GetLowDataQualityPOIs(List<ChargePoint> allPOIs)
        {
            return allPOIs.Where(p => p.AddressInfo == null ||
                p.AddressInfo != null &&
                (String.IsNullOrEmpty(p.AddressInfo.Title)
                ||
                String.IsNullOrEmpty(p.AddressInfo.AddressLine1) && String.IsNullOrEmpty(p.AddressInfo.Postcode)
                ||
                (p.AddressInfo.CountryID == null && p.AddressInfo.Country == null))
                ).ToList();
        }

        /// <summary>
        /// Determine if 2 CPs have the same location details or very close lat/lng
        /// </summary>
        /// <param name="current"></param>
        /// <param name="previous"></param>
        /// <returns></returns>
        public bool IsDuplicateLocation(ChargePoint current, ChargePoint previous, bool compareTitle)
        {
            //is duplicate item if latlon is exact match for previous item or latlon is within few meters of previous item
            if (
                (GeoManager.IsClose(current.AddressInfo.Latitude, current.AddressInfo.Longitude, previous.AddressInfo.Latitude, previous.AddressInfo.Longitude) && new System.Device.Location.GeoCoordinate(current.AddressInfo.Latitude, current.AddressInfo.Longitude).GetDistanceTo(new System.Device.Location.GeoCoordinate(previous.AddressInfo.Latitude, previous.AddressInfo.Longitude)) < DUPLICATE_DISTANCE_METERS) //meters distance apart
                || (compareTitle && (previous.AddressInfo.Title == current.AddressInfo.Title))
                //&& previous.AddressInfo.AddressLine1 == current.AddressInfo.AddressLine1
                || (previous.AddressInfo.Latitude == current.AddressInfo.Latitude && previous.AddressInfo.Longitude == current.AddressInfo.Longitude)
                || (current.DataProvidersReference != null && current.DataProvidersReference.Length > 0 && previous.DataProvidersReference == current.DataProvidersReference)
                || (previous.AddressInfo.ToString() == current.AddressInfo.ToString())

                )
            {
                int dataProviderId = (previous.DataProvider != null ? previous.DataProvider.ID : (int)previous.DataProviderID);
                if (previous.AddressInfo.Latitude == current.AddressInfo.Latitude && previous.AddressInfo.Longitude == current.AddressInfo.Longitude)
                {
                    Log(current.AddressInfo.ToString() + " is Duplicate due to exact equal latlon to [Data Provider " + dataProviderId + "]" + previous.AddressInfo.ToString());
                }
                else if (new System.Device.Location.GeoCoordinate(current.AddressInfo.Latitude, current.AddressInfo.Longitude).GetDistanceTo(new System.Device.Location.GeoCoordinate(previous.AddressInfo.Latitude, previous.AddressInfo.Longitude)) < DUPLICATE_DISTANCE_METERS)
                {
                    Log(current.AddressInfo.ToString() + " is Duplicate due to close proximity to [Data Provider " + dataProviderId + "]" + previous.AddressInfo.ToString());
                }
                else if (previous.AddressInfo.Title == current.AddressInfo.Title && previous.AddressInfo.AddressLine1 == current.AddressInfo.AddressLine1 && previous.AddressInfo.Postcode == current.AddressInfo.Postcode)
                {
                    Log(current.AddressInfo.ToString() + " is Duplicate due to same Title and matching AddressLine1 and Postcode  [Data Provider " + dataProviderId + "]" + previous.AddressInfo.ToString());
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool MergeItemChanges(ChargePoint sourceItem, ChargePoint destItem, bool statusOnly)
        {
            //TODO: move to POIManager or a common base?
            bool hasDifferences = true;
            //var diffs = GetDifferingProperties(sourceItem, destItem);

            //merge changes in sourceItem into destItem, preserving destItem ID's
            if (!statusOnly)
            {
                //update addressinfo
                destItem.AddressInfo.Title = sourceItem.AddressInfo.Title;
                destItem.AddressInfo.AddressLine1 = sourceItem.AddressInfo.AddressLine1;
                destItem.AddressInfo.AddressLine2 = sourceItem.AddressInfo.AddressLine2;
                destItem.AddressInfo.Town = sourceItem.AddressInfo.Town;
                destItem.AddressInfo.StateOrProvince = sourceItem.AddressInfo.StateOrProvince;
                destItem.AddressInfo.Postcode = sourceItem.AddressInfo.Postcode;
                destItem.AddressInfo.RelatedURL = sourceItem.AddressInfo.RelatedURL;
                if (sourceItem.AddressInfo.Country != null) destItem.AddressInfo.Country = sourceItem.AddressInfo.Country;
                destItem.AddressInfo.CountryID = sourceItem.AddressInfo.CountryID;

                destItem.AddressInfo.AccessComments = sourceItem.AddressInfo.AccessComments;
                destItem.AddressInfo.ContactEmail = sourceItem.AddressInfo.ContactEmail;
                destItem.AddressInfo.ContactTelephone1 = sourceItem.AddressInfo.ContactTelephone1;
                destItem.AddressInfo.ContactTelephone2 = sourceItem.AddressInfo.ContactTelephone2;
#pragma warning disable 0612
                destItem.AddressInfo.GeneralComments = sourceItem.AddressInfo.GeneralComments;
#pragma warning restore 0612
                destItem.AddressInfo.Latitude = sourceItem.AddressInfo.Latitude;
                destItem.AddressInfo.Longitude = sourceItem.AddressInfo.Longitude;

                //update general
                destItem.DataProvider = sourceItem.DataProvider;
                destItem.DataProviderID = sourceItem.DataProviderID;
                destItem.DataProvidersReference = sourceItem.DataProvidersReference;
                destItem.OperatorID = sourceItem.OperatorID;
                destItem.OperatorInfo = sourceItem.OperatorInfo;
                destItem.OperatorsReference = sourceItem.OperatorsReference;
                destItem.UsageType = sourceItem.UsageType;
                destItem.UsageTypeID = sourceItem.UsageTypeID;
                destItem.UsageCost = sourceItem.UsageCost;
                destItem.NumberOfPoints = sourceItem.NumberOfPoints;
                destItem.GeneralComments = sourceItem.GeneralComments;
                destItem.DateLastConfirmed = sourceItem.DateLastConfirmed;
                //destItem.DateLastStatusUpdate = sourceItem.DateLastStatusUpdate;
                destItem.DatePlanned = sourceItem.DatePlanned;

                //update connections
                //TODO:: update connections
                var connDeleteList = new List<ConnectionInfo>();
                if (sourceItem.Connections != null)
                {
                    if (destItem.Connections == null)
                    {
                        destItem.Connections = sourceItem.Connections;
                    }
                    else
                    {
                        var equipmentIndex = 0;
                        foreach (var conn in sourceItem.Connections)
                        {
                            //imported equipment info is replaced in order they appear in the import
                            //TODO: if the connection type/power rating has changed we need to create new equipment
                            // rather than update existing as this is probably a physically different equipment installation

                            ConnectionInfo existingConnection = null;

                            existingConnection = destItem.Connections.FirstOrDefault(d => d.Reference == conn.Reference && !String.IsNullOrEmpty(conn.Reference));
                            if (existingConnection == null)
                            {
                                if (destItem.Connections != null && destItem.Connections.Count >= (equipmentIndex + 1))
                                {
                                    existingConnection = destItem.Connections[equipmentIndex];
                                }
                                equipmentIndex++;
                            }

                            if (existingConnection != null)
                            {
                                //update existing- updates can be either object base reference data or use ID values
                                existingConnection.ConnectionType = conn.ConnectionType;
                                existingConnection.ConnectionTypeID = conn.ConnectionTypeID;
                                existingConnection.Quantity = conn.Quantity;
                                existingConnection.LevelID = conn.LevelID;
                                existingConnection.Level = conn.Level;
                                existingConnection.Reference = conn.Reference;
                                existingConnection.StatusTypeID = conn.StatusTypeID;
                                existingConnection.StatusType = conn.StatusType;
                                existingConnection.Voltage = conn.Voltage;
                                existingConnection.Amps = conn.Amps;
                                existingConnection.PowerKW = conn.PowerKW;
                                existingConnection.Comments = conn.Comments;
                                existingConnection.CurrentType = conn.CurrentType;
                                existingConnection.CurrentTypeID = conn.CurrentTypeID;
                            }
                            else
                            {
                                //add new
                                destItem.Connections.Add(conn);
                            }
                        }
                    }
                }
            }
            if (sourceItem.StatusType != null) destItem.StatusType = sourceItem.StatusType;

            return hasDifferences;
        }

        public List<IImportProvider> GetImportProviders(List<OCM.API.Common.Model.DataProvider> AllDataProviders)
        {
            List<IImportProvider> providers = new List<IImportProvider>();

            providers.Add(new ImportProvider_UKChargePointRegistry());
            providers.Add(new ImportProvider_CarStations());
            providers.Add(new ImportProvider_Mobie());
            providers.Add(new ImportProvider_AFDC());
            providers.Add(new ImportProvider_ESB_eCars());
            providers.Add(new ImportProvider_AddEnergie(ImportProvider_AddEnergie.NetworkType.LeCircuitElectrique));
            providers.Add(new ImportProvider_AddEnergie(ImportProvider_AddEnergie.NetworkType.ReseauVER));
            providers.Add(new ImportProvider_NobilDotNo());
            providers.Add(new ImportProvider_OplaadpalenNL());

            //populate full data provider details for each import provider
            foreach (var provider in providers)
            {
                var providerDetails = (BaseImportProvider)provider;
                var dataProviderDetails = AllDataProviders.FirstOrDefault(p => p.ID == providerDetails.DataProviderID);
                if (dataProviderDetails != null)
                {
                    providerDetails.DefaultDataProvider = dataProviderDetails;
                }
            }
            return providers;
        }

        public async Task<bool> PerformImportProcessing(ExportType exportType, string defaultDataPath, string apiIdentifier, string apiSessionToken, bool fetchLiveData)
        {
            OCMClient client = new OCMClient(IsSandboxedAPIMode);
            var credentials = GetAPISessionCredentials(apiIdentifier, apiSessionToken);

            CoreReferenceData coreRefData = null;
            coreRefData = await client.GetCoreReferenceData();

            string outputPath = "Data\\";
            List<IImportProvider> providers = new List<IImportProvider>();

            string inputDataPathPrefix = defaultDataPath;

            foreach (var provider in providers)
            {
                await PerformImport(exportType, fetchLiveData, credentials, coreRefData, outputPath, provider, false);
            }

            return true;
        }

        public async Task<ImportReport> PerformImport(ExportType exportType, bool fetchLiveData, APICredentials credentials, CoreReferenceData coreRefData, string outputPath, IImportProvider provider, bool cacheInputData)
        {
            var p = ((BaseImportProvider)provider);
            p.ExportType = exportType;

            ImportReport resultReport = new ImportReport();
            resultReport.ProviderDetails = p;

            try
            {
                bool loadOK = false;
                if (p.ImportInitialisationRequired && p is IImportProviderWithInit)
                {
                    ((IImportProviderWithInit)provider).InitImportProvider();
                }
                if (fetchLiveData && p.IsAutoRefreshed && !String.IsNullOrEmpty(p.AutoRefreshURL))
                {
                    Log("Loading input data from URL..");
                    loadOK = p.LoadInputFromURL(p.AutoRefreshURL);
                }
                else
                {
                    if (p.IsStringData && !p.UseCustomReader)
                    {
                        Log("Loading input data from file..");
                        loadOK = p.LoadInputFromFile(p.InputPath);
                    }
                    else
                    {
                        //binary streams pass as OK by default
                        loadOK = true;
                    }
                }

                if (!loadOK)
                {
                    //failed to load
                    Log("Failed to load input data.");
                    throw new Exception("Failed to fetch input data");
                }
                else
                {
                    if (fetchLiveData && cacheInputData)
                    {
                        //save input data
                        p.SaveInputFile(p.InputPath);
                    }
                }

                List<ChargePoint> duplicatesList = new List<ChargePoint>();

                Log("Processing input..");

                var list = provider.Process(coreRefData);

                int numAdded = 0;
                int numUpdated = 0;

                if (list.Count > 0)
                {
                    if (p.MergeDuplicatePOIEquipment)
                    {
                        Log("Merging Equipment from Duplicate POIs");
                        list = MergeDuplicatePOIEquipment(list);
                    }

                    if (!p.IncludeInvalidPOIs)
                    {
                        Log("Cleaning invalid POIs");
                        var invalidPOIs = new List<ChargePoint>();
                        foreach (var poi in list)
                        {
                            if (!BaseImportProvider.IsPOIValidForImport(poi))
                            {
                                invalidPOIs.Add(poi);
                            }
                        }
                        foreach (var poi in invalidPOIs)
                        {
                            list.Remove(poi);
                        }
                    }

                    List<ChargePoint> finalList = new List<ChargePoint>();

                    if (!p.SkipDeduplication)
                    {
                        Log("De-Deuplicating list (" + p.ProviderName + ":: " + list.Count + " Items)..");

                        //de-duplicate and clean list based on existing data
                        //TODO: take original and replace in final update list, setting relevant updated properties (merge) and status
                        finalList = await DeDuplicateList(list.ToList(), true, coreRefData, resultReport, p.AllowDuplicatePOIWithDifferentOperator);
                        //var finalList = list;
                    }
                    else
                    {
                        //skip deduplication
                        finalList = list.ToList();
                    }

                    if (ImportUpdatesOnly)
                    {
                        finalList = finalList.Where(l => l.ID > 0).ToList();
                    }
                    //finalList = client.GetLocations(new SearchFilters { MaxResults = 10000 });

                    //export/apply updates
                    if (p.ExportType == ExportType.XML)
                    {
                        Log("Exporting XML..");

                        //output xml
                        p.ExportXMLFile(finalList, outputPath + p.OutputNamePrefix + ".xml");
                    }

                    if (p.ExportType == ExportType.CSV)
                    {
                        Log("Exporting CSV..");
                        //output csv
                        p.ExportCSVFile(finalList, outputPath + p.OutputNamePrefix + ".csv");
                    }

                    if (p.ExportType == ExportType.JSON)
                    {
                        Log("Exporting JSON..");
                        //output json
                        p.ExportJSONFile(finalList, outputPath + p.OutputNamePrefix + ".json");
                    }
                    if (p.ExportType == ExportType.API && p.IsProductionReady)
                    {
                        //publish list of locations to OCM via API
                        OCMClient ocmClient = new OCMClient(IsSandboxedAPIMode);
                        Log("Publishing via API..");
                        foreach (ChargePoint cp in finalList.Where(l => l.AddressInfo.Country != null))
                        {
                            ocmClient.UpdateItem(cp, credentials);
                            if (cp.ID == 0)
                            {
                                numAdded++;
                            }
                            else
                            {
                                numUpdated++;
                            }
                        }
                    }
                    if (p.ExportType == ExportType.POIModelList)
                    {
                        //result report contains POI lists
                    }
                }

                Log("Import Processed:" + provider.GetProviderName() + " Added:" + numAdded + " Updated:" + numUpdated);
            }
            catch (Exception exp)
            {
                Log("Import Failed:" + provider.GetProviderName() + " ::" + exp.ToString());
            }

            resultReport.Log = "";
            resultReport.Log += p.ProcessingLog;

            resultReport.Log += ImportLog;
            return resultReport;
        }

        /// <summary>
        /// For a list of ChargePoint objects, attempt to populate the AddressInfo (at least country) based on lat/lon if not already populated
        /// </summary>
        /// <param name="itemList"></param>
        /// <param name="coreRefData"></param>

        public void UpdateImportedPOIList(ImportReport poiResults, User user)
        {
            var submissionManager = new SubmissionManager();
            int itemCount = 1;
            foreach (var newPOI in poiResults.Added)
            {
                Log("Importing New POI " + itemCount + ": " + newPOI.AddressInfo.ToString());
                submissionManager.PerformPOISubmission(newPOI, user, false);
            }

            foreach (var updatedPOI in poiResults.Updated)
            {
                Log("Importing Updated POI " + itemCount + ": " + updatedPOI.AddressInfo.ToString());
                submissionManager.PerformPOISubmission(updatedPOI, user, performCacheRefresh: false, disablePOISuperseding: true);
            }

            foreach (var delisted in poiResults.Delisted)
            {
                Log("Delisting Removed POI " + itemCount + ": " + delisted.AddressInfo.ToString());
                delisted.SubmissionStatus = null;
                delisted.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Delisted_RemovedByDataProvider;

                submissionManager.PerformPOISubmission(delisted, user, false);
            }

            //refresh POI cache
            var cacheTask = Task.Run(async () =>
            {
                return await OCM.Core.Data.CacheManager.RefreshCachedData();
            });
            cacheTask.Wait();

            //temp get all providers references for recognised duplicates
            /*var dupeRefs = from dupes in poiResults.Duplicates
                           where !String.IsNullOrEmpty(dupes.DataProvidersReference)
                           select dupes.DataProvidersReference;
            string dupeOutput = "";
            foreach(var d in dupeRefs)
            {
                dupeOutput += ",'"+d+"'";
            }
            System.Diagnostics.Debug.WriteLine(dupeOutput);*/

            if (poiResults.ProviderDetails.DefaultDataProvider != null)
            {
                //update date last updated for this provider
                new DataProviderManager().UpdateDateLastImport(poiResults.ProviderDetails.DefaultDataProvider.ID);
            }
        }

        #region helper methods

        public void PopulateLocationFromGeolocationCache(List<ChargePoint> itemList, CoreReferenceData coreRefData)
        {
            OCM.Import.Analysis.SpatialAnalysis spatialAnalysis = new Analysis.SpatialAnalysis(TempFolder + "\\Shapefiles\\World\\ne_10m_admin_0_map_units.shp");

            //process list of locations, populating country refreshing cache where required
            foreach (var item in itemList)
            {
                if (item.AddressInfo.Country == null && item.AddressInfo.CountryID == null)
                {
                    Country country = null;

                    var test = spatialAnalysis.ClassifyPoint((double)item.AddressInfo.Latitude, (double)item.AddressInfo.Longitude);
                    if (test != null)
                    {
                        country = coreRefData.Countries.FirstOrDefault(c => c.ISOCode == test.CountryCode || c.Title == test.CountryName);
                    }
                    if (country == null)
                    {
                        var geoLookup = geolocationCacheManager.PerformLocationLookup((double)item.AddressInfo.Latitude, (double)item.AddressInfo.Longitude, coreRefData.Countries);
                        if (geoLookup != null)
                        {
                            country = coreRefData.Countries.FirstOrDefault(c => c.ID == geoLookup.CountryID || c.ISOCode == geoLookup.CountryCode || c.Title == geoLookup.CountryName);
                        }
                    }
                    if (country != null)
                    {
                        item.AddressInfo.Country = country;

                        //remove country name from address line 1 if present
                        if (item.AddressInfo.AddressLine1 != null)
                        {
                            if (item.AddressInfo.AddressLine1.ToLower().Contains(country.Title.ToLower()))
                            {
                                item.AddressInfo.AddressLine1 = item.AddressInfo.AddressLine1.Replace(country.Title, "").Trim();
                            }
                        }
                    }

                    if (item.AddressInfo.Country == null)
                    {
                        LogHelper.Log("Failed to resolve country for item:" + item.AddressInfo.Title);
                    }
                    else
                    {
                        item.AddressInfo.CountryID = item.AddressInfo.Country.ID;
                    }
                }
            }

            //cache may have updates, save for next time
            geolocationCacheManager.SaveCache();
        }

        public APICredentials GetAPISessionCredentials(string identifier, string sessionToken)
        {
            return new APICredentials { Identifier = identifier, SessionToken = sessionToken };
        }

        public void GeocodingTest()
        {
            OCMClient client = new OCMClient(IsSandboxedAPIMode);

            //get a few OCM listings
            SearchFilters filters = new SearchFilters { SubmissionStatusTypeIDs = new int[] { (int)StandardSubmissionStatusTypes.Submitted_Published }, CountryIDs = new int[] { 1 }, DataProviderIDs = new int[] { 1 }, MaxResults = 2000, EnableCaching = false };

            var poiList = client.GetLocations(filters);
            /*
            GeocodingService g = new GeocodingService();
            List<GeolocationResult> list = new List<GeolocationResult>();

            //attempt OSM geocoding
            foreach (var poi in poiList)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("OCM-" + poi.ID + " : [" + poi.AddressInfo.Title + "] " + poi.AddressInfo.ToString());

                    System.Diagnostics.Debug.WriteLine("OCM : LL: " + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude);

                    var osm = g.GeolocateAddressInfo_OSM(poi.AddressInfo);
                    System.Diagnostics.Debug.WriteLine("OSM : LL: " + osm.Latitude + "," + osm.Longitude);
                    list.Add(osm);

                    var mpq = g.GeolocateAddressInfo_MapquestOSM(poi.AddressInfo);
                    System.Diagnostics.Debug.WriteLine("MPQ : LL: " + mpq.Latitude + "," + mpq.Longitude);
                    list.Add(mpq);
                }
                catch (Exception exp)
                {
                    System.Diagnostics.Debug.WriteLine("Exception during geocoding:" + exp.ToString());
                }
                System.Threading.Thread.Sleep(1000);
            }

            string json = JsonConvert.SerializeObject(list, Formatting.Indented);
            System.IO.File.WriteAllText("C:\\temp\\GeocodingResult.json", json);
             * */
        }

        #endregion helper methods
    }
}