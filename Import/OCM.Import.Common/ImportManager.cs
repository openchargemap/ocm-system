using KellermanSoftware.CompareNetObjects;
using Newtonsoft.Json;
using OCM.API.Client;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.Import.Misc;
using OCM.Import.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.Import
{
    public class ImportReport
    {
        public BaseImportProvider ProviderDetails { get; set; }

        public List<ChargePoint> Added { get; set; }

        public List<ChargePoint> Updated { get; set; }

        public List<ChargePoint> Unchanged { get; set; }

        public List<ChargePoint> LowDataQuality { get; set; }

        public List<ChargePoint> Delisted { get; set; }

        public List<ChargePoint> Duplicates { get; set; }

        public ImportReport()
        {
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

        public async Task<List<ChargePoint>> DeDuplicateList(List<ChargePoint> cpList, bool updateDuplicate, CoreReferenceData coreRefData, ImportReport report)
        {
            //get list of all current POIs (in relevant countries) including most delisted ones
            int[] countryIds = (from poi in cpList
                                where poi.AddressInfo.Country != null
                                select poi.AddressInfo.Country.ID).Distinct().ToArray();

            APIRequestSettings filters = new APIRequestSettings { CountryIDs = countryIds, MaxResults = 1000000, EnableCaching = false, SubmissionStatusTypeID = 0 };
            //List<ChargePoint> masterList = await new OCMClient(IsSandboxedAPIMode).GetLocations(filters); //new OCMClient().FindSimilar(null, 10000); //fetch all charge points regardless of status
            var poiManager = new POIManager();

            List<ChargePoint> masterList = poiManager.GetChargePoints(filters); //new OCMClient().FindSimilar(null, 10000); //fetch all charge points regardless of status
            List<ChargePoint> masterListCopy = JsonConvert.DeserializeObject<List<ChargePoint>>(JsonConvert.SerializeObject(masterList)); //new OCMClient().FindSimilar(null, 10000); //fetch all charge points regardless of status

            //if we failed to get a master list, quit with no result
            if (masterList.Count == 0) return new List<ChargePoint>();

            List<ChargePoint> duplicateList = new List<ChargePoint>();
            List<ChargePoint> updateList = new List<ChargePoint>();

            ChargePoint previousCP = null;

            //for each item to be imported, deduplicate by adding to updateList only the items which we don't already have

            var cpListSortedByPos = cpList.OrderBy(c => c.AddressInfo.Latitude).ThenBy(c => c.AddressInfo.Longitude);

            foreach (var item in cpListSortedByPos)
            {
                var itemGeoPos = new System.Device.Location.GeoCoordinate(item.AddressInfo.Latitude, item.AddressInfo.Longitude);

                //item is duplicate if we already seem to have it based on Data Providers reference or approx position match
                var dupeList = masterList.Where(c =>
                        (c.DataProvider != null && c.DataProvider.ID == item.DataProvider.ID && c.DataProvidersReference == item.DataProvidersReference)
                        || (c.AddressInfo.Title == item.AddressInfo.Title && c.AddressInfo.AddressLine1 == item.AddressInfo.AddressLine1 && c.AddressInfo.Postcode == item.AddressInfo.Postcode)
                        || new System.Device.Location.GeoCoordinate(c.AddressInfo.Latitude, c.AddressInfo.Longitude).GetDistanceTo(itemGeoPos) < DUPLICATE_DISTANCE_METERS //meters distance apart
                );

                if (dupeList.Count() > 0)
                {
                    if (updateDuplicate)
                    {
                        //if updating duplicates, get exact matching duplicate based on provider reference and update/merge with this item to update status/merge properties
                        var updatedItem = dupeList.FirstOrDefault(d => d.DataProvider.ID == item.DataProvider.ID && d.DataProvidersReference == item.DataProvidersReference);
                        if (updatedItem != null)
                        {
                            //only merge/update from live published items
                            if (updatedItem.SubmissionStatus.IsLive == (bool?)true)
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
                    if (IsDuplicateLocation(item, previousCP, true))
                    {
                        if (!duplicateList.Contains(item))
                        {
                            System.Diagnostics.Debug.WriteLine("Duplicated item removed:" + item.AddressInfo.Title);
                            duplicateList.Add(item);
                        }
                    }
                }
                previousCP = item;
            }

            //remove duplicates from list to apply
            foreach (var dupe in duplicateList)
            {
                cpList.Remove(dupe);
            }

            System.Diagnostics.Debug.WriteLine("Duplicate removed from import:" + duplicateList.Count);

            //add updated items (replace duplicates with property changes)

            foreach (var updatedItem in updateList)
            {
                if (!cpList.Contains(updatedItem))
                {
                    cpList.Add(updatedItem);
                }
            }

            System.Diagnostics.Debug.WriteLine("Updated items to import:" + updateList.Count);

            //populate missing location info from geolocation cache if possible
            PopulateLocationFromGeolocationCache(cpList, coreRefData);

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
            report.Delisted = masterList.Where(cp => cp.DataProviderID == report.ProviderDetails.DataProviderID && cp.SubmissionStatus != null && cp.SubmissionStatus.IsLive == true
                && !cpListSortedByPos.Any(master => master.ID == cp.ID) && !report.Duplicates.Any(master => master.ID == cp.ID)).ToList();

            //determine list of low quality POIs (incomplete address info etc)
            report.LowDataQuality = new List<ChargePoint>();
            report.LowDataQuality.AddRange(GetLowDataQualityPOIs(report.Added));
            report.LowDataQuality.AddRange(GetLowDataQualityPOIs(report.Updated));

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
                    System.Diagnostics.Debug.WriteLine("Difference:"+diffReport.OutputString(result.Differences));

                }
            }

            foreach (var p in updatesToIgnore)
            {
                if (report.Unchanged == null) report.Unchanged = new List<ChargePoint>();
                report.Unchanged.Add(p);
                report.Updated.Remove(p);
            }

            //TODO: if POi is a duplicate ensure imported data provider reference/URL  is included as reference metadata in OCM's version of the POI

            //return final processed list ready for applying as insert/updates
            return cpListSortedByPos.ToList();
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
                p.AddressInfo.Country == null)
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
                (compareTitle && (previous.AddressInfo.Title == current.AddressInfo.Title))
                //&& previous.AddressInfo.AddressLine1 == current.AddressInfo.AddressLine1
                || (previous.AddressInfo.Latitude == current.AddressInfo.Latitude && previous.AddressInfo.Longitude == current.AddressInfo.Longitude)
                || (current.DataProvidersReference != null && current.DataProvidersReference.Length > 0 && previous.DataProvidersReference == current.DataProvidersReference)
                || (previous.AddressInfo.ToString() == current.AddressInfo.ToString())
                || new System.Device.Location.GeoCoordinate(current.AddressInfo.Latitude, current.AddressInfo.Longitude).GetDistanceTo(new System.Device.Location.GeoCoordinate(previous.AddressInfo.Latitude, previous.AddressInfo.Longitude)) < DUPLICATE_DISTANCE_METERS //meters distance apart
                )
            {
                if (previous.AddressInfo.Latitude == current.AddressInfo.Latitude && previous.AddressInfo.Longitude == current.AddressInfo.Longitude)
                {
                    System.Diagnostics.Debug.WriteLine(current.AddressInfo.ToString() + " is Duplicate due to exact equal latlon to [Data Provider " + previous.DataProvider.ID + "]" + previous.AddressInfo.ToString());
                }
                else if (new System.Device.Location.GeoCoordinate(current.AddressInfo.Latitude, current.AddressInfo.Longitude).GetDistanceTo(new System.Device.Location.GeoCoordinate(previous.AddressInfo.Latitude, previous.AddressInfo.Longitude)) < DUPLICATE_DISTANCE_METERS)
                {
                    System.Diagnostics.Debug.WriteLine(current.AddressInfo.ToString() + " is Duplicate due to close proximity to [Data Provider " + previous.DataProvider.ID + "]" + previous.AddressInfo.ToString());
                }
                else if (previous.AddressInfo.Title == current.AddressInfo.Title && previous.AddressInfo.AddressLine1 == current.AddressInfo.AddressLine1 && previous.AddressInfo.Postcode == current.AddressInfo.Postcode)
                {
                    System.Diagnostics.Debug.WriteLine(current.AddressInfo.ToString() + " is Duplicate due to same Title and matching AddressLine1 and Postcode  [Data Provider " + previous.DataProvider.ID + "]" + previous.AddressInfo.ToString());
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
                destItem.DataProvidersReference = sourceItem.DataProvidersReference;
                destItem.OperatorInfo = sourceItem.OperatorInfo;
                destItem.OperatorsReference = sourceItem.OperatorsReference;
                destItem.UsageType = sourceItem.UsageType;
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
                                //update existing
                                if (conn.ConnectionType != null) existingConnection.ConnectionType = conn.ConnectionType;
                                existingConnection.Quantity = conn.Quantity;
                                existingConnection.Level = conn.Level;
                                existingConnection.Reference = conn.Reference;
                                existingConnection.StatusType = conn.StatusType;
                                existingConnection.Voltage = conn.Voltage;
                                existingConnection.Amps = conn.Amps;
                                existingConnection.PowerKW = conn.PowerKW;
                                existingConnection.Comments = conn.Comments;
                                if (conn.CurrentType != null) existingConnection.CurrentType = conn.CurrentType;
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
            //providers.Add(new ImportProvider_BlinkNetwork() { InputPath = inputDataPathPrefix + "blinknetwork.com\\jsondata2.txt" });
            providers.Add(new ImportProvider_CarStations());
            //providers.Add(new ImportProvider_RWEMobility() { InputPath = inputDataPathPrefix + "rwe-mobility\\data.json.txt" });

            providers.Add(new ImportProvider_Mobie());
            providers.Add(new ImportProvider_AFDC());
            providers.Add(new ImportProvider_ESB_eCars());
            providers.Add(new ImportProvider_AddEnergie(ImportProvider_AddEnergie.NetworkType.LeCircuitElectrique));
            providers.Add(new ImportProvider_AddEnergie(ImportProvider_AddEnergie.NetworkType.ReseauVER));

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

            // providers.Add(new ImportProvider_RWEMobility() { InputPath = inputDataPathPrefix + "rwe-mobility\\data.json.txt" });

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

                if (fetchLiveData && p.IsAutoRefreshed && !String.IsNullOrEmpty(p.AutoRefreshURL))
                {
                    p.Log("Loading input data from URL..");
                    loadOK = p.LoadInputFromURL(p.AutoRefreshURL);
                }
                else
                {
                    if (p.IsStringData)
                    {
                        p.Log("Loading input data from file..");
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
                    p.Log("Failed to load input data.");
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

                p.Log("Processing input..");

                var list = provider.Process(coreRefData);

                int numAdded = 0;
                int numUpdated = 0;

                if (list.Count > 0)
                {
                    p.Log("Cleaning invalid POIs");
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

                    p.Log("De-Deuplicating list (" + p.ProviderName + ":: " + list.Count + " Items)..");

                    //de-duplicate and clean list based on existing data
                    //TODO: take original and replace in final update list, setting relevant updated properties (merge) and status
                    var finalList = await DeDuplicateList(list.ToList(), true, coreRefData, resultReport);
                    //var finalList = list;

                    if (ImportUpdatesOnly)
                    {
                        finalList = finalList.Where(l => l.ID > 0).ToList();
                    }
                    //finalList = client.GetLocations(new SearchFilters { MaxResults = 10000 });

                    //export/apply updates
                    if (p.ExportType == ExportType.XML)
                    {
                        p.Log("Exporting XML..");

                        //output xml
                        p.ExportXMLFile(finalList, outputPath + p.OutputNamePrefix + ".xml");
                    }

                    if (p.ExportType == ExportType.CSV)
                    {
                        p.Log("Exporting CSV..");
                        //output csv
                        p.ExportCSVFile(finalList, outputPath + p.OutputNamePrefix + ".csv");
                    }

                    if (p.ExportType == ExportType.JSON)
                    {
                        p.Log("Exporting JSON..");
                        //output json
                        p.ExportJSONFile(finalList, outputPath + p.OutputNamePrefix + ".json");
                    }
                    if (p.ExportType == ExportType.API && p.IsProductionReady)
                    {
                        //publish list of locations to OCM via API
                        OCMClient ocmClient = new OCMClient(IsSandboxedAPIMode);
                        p.Log("Publishing via API..");
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

                p.Log("Import Processed:" + provider.GetProviderName() + " Added:" + numAdded + " Updated:" + numUpdated);
            }
            catch (Exception exp)
            {
                p.Log("Import Failed:" + provider.GetProviderName() + " ::" + exp.ToString());
            }

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
                System.Diagnostics.Debug.WriteLine("Importing New POI " + itemCount + ": " + newPOI.AddressInfo.ToString());
                submissionManager.PerformPOISubmission(newPOI, user, false);
            }

            foreach (var updatedPOI in poiResults.Updated)
            {
                System.Diagnostics.Debug.WriteLine("Importing Updated POI " + itemCount + ": " + updatedPOI.AddressInfo.ToString());
                submissionManager.PerformPOISubmission(updatedPOI, user, performCacheRefresh: false, disablePOISuperseding: true);
            }

            /*
            foreach (var deletedPOI in poiResults.Delisted)
            {
                System.Diagnostics.Debug.WriteLine("Delisting Removed POI " + itemCount + ": " + deletedPOI.AddressInfo.ToString());
                deletedPOI.SubmissionStatus = null;
                deletedPOI.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Delisted_NoLongerActive;

                submissionManager.PerformPOISubmission(deletedPOI, user, false);
            }*/

            //refresh POI cache
            var cacheTask = Task.Run(async () =>
            {
                return await OCM.Core.Data.CacheManager.RefreshCachedPOIList();
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
            //process list of locations, populating country refreshing cache where required
            foreach (var item in itemList)
            {
                if (item.AddressInfo.Country == null)
                {
                    var geoLookup = geolocationCacheManager.PerformLocationLookup((double)item.AddressInfo.Latitude, (double)item.AddressInfo.Longitude, coreRefData.Countries);
                    if (geoLookup != null)
                    {
                        var country = coreRefData.Countries.FirstOrDefault(c => c.ID == geoLookup.CountryID || c.ISOCode == geoLookup.CountryCode || c.Title == geoLookup.CountryName);
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
                    }
                }

                if (item.AddressInfo.Country == null)
                {
                    LogHelper.Log("Failed to resolve country for item:" + item.AddressInfo.Title);
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