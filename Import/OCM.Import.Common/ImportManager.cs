using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetProjects.IndexedLinq;
using GeoCoordinatePortable;
using KellermanSoftware.CompareNetObjects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCM.API.Client;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.Import.Misc;
using OCM.Import.Providers;
using OCM.Import.Providers.OCPI;

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

        public bool IsSuccess { get; set; } = false;

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

    public class ImportProcessSettings
    {
        public ExportType ExportType { get; set; }
        public string DefaultDataPath { get; set; }
        public string ApiIdentifier { get; set; }
        public string ApiSessionToken { get; set; }
        public bool FetchLiveData { get; set; } = true;
        public bool CacheInputData { get; set; } = true;
        public bool FetchExistingFromAPI { get; set; } = false;
        public bool PerformDeduplication { get; set; } = true;
        public string ProviderName { get; set; }

        public Dictionary<string, string> Credentials { get; set; }
    }

    public class ImportManager
    {
        public const int DUPLICATE_DISTANCE_METERS = 50;

        public bool ImportUpdatesOnly { get; set; }

        public bool IsSandboxedAPIMode { get; set; }

        public string GeonamesAPIUserName { get; set; }

        public string TempFolder { get; set; }

        private GeolocationCacheManager geolocationCacheManager = null;
        private AddressLookupCacheManager addressLookupCacheManager = null;

        public string ImportLog { get; set; }

        public bool UseDataModelComparison { get; set; } = false;

        private OCMClient _client = null;

        private ImportSettings _settings;
        private Microsoft.Extensions.Logging.ILogger _log;

        public ImportManager(ImportSettings settings, string apiKey = null, Microsoft.Extensions.Logging.ILogger log = null)
        {
            _settings = settings;
            _log = log;

            GeonamesAPIUserName = "openchargemap";
            TempFolder = _settings.TempFolderPath;

            _client = new OCMClient(_settings.MasterAPIBaseUrl, apiKey, log, settings.ImportUserAgent);

            geolocationCacheManager = new GeolocationCacheManager(TempFolder);
            geolocationCacheManager.GeonamesAPIUserName = GeonamesAPIUserName;
            geolocationCacheManager.LoadCache();

            addressLookupCacheManager = new AddressLookupCacheManager(TempFolder, _client);
        }

        public OCMClient OCMClient => _client;

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

        public List<IImportProvider> GetImportProviders(List<OCM.API.Common.Model.DataProvider> AllDataProviders, ImportProcessSettings importSettings)
        {
            List<IImportProvider> providers = new List<IImportProvider>();

            providers.Add(new ImportProvider_UKChargePointRegistry());
            providers.Add(new ImportProvider_Mobie());

            if (importSettings.Credentials.TryGetValue("IMPORT-adfc", out var afdcKey))
            {
                providers.Add(new ImportProvider_AFDC(afdcKey));
            }

            providers.Add(new ImportProvider_ESB_eCars());

            if (_settings.ApiKeys.TryGetValue("addenergie_le", out var ae_le))
            {
                providers.Add(new ImportProvider_AddEnergie(ImportProvider_AddEnergie.NetworkType.LeCircuitElectrique, ae_le));
            }

            if (_settings.ApiKeys.TryGetValue("addenergie_re", out var ae_re))
            {
                providers.Add(new ImportProvider_AddEnergie(ImportProvider_AddEnergie.NetworkType.ReseauVER, ae_re));
            }

            if (_settings.ApiKeys.TryGetValue("nobil_no", out var nobil))
            {
                providers.Add(new ImportProvider_NobilDotNo(nobil));
            }

            providers.Add(new ImportProvider_ICAEN());
            providers.Add(new ImportProvider_GenericExcel());
            providers.Add(new ImportProvider_GoEvio());
            providers.Add(new ImportProvider_Sitronics());
            providers.Add(new ImportProvider_Lakd());
            providers.Add(new ImportProvider_Gaia());
            providers.Add(new ImportProvider_Toger());
            providers.Add(new ImportProvider_ElectricEra());
            providers.Add(new ImportProvider_ITCharge());
            providers.Add(new ImportProvider_Voltrelli());

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

        public async Task<bool> PerformImportProcessing(ImportProcessSettings settings)
        {

            var credentials = GetAPISessionCredentials("System", settings.Credentials["IMPORT-ocm-system"]);

            var coreRefData = await _client.GetCoreReferenceDataAsync();

            var providers = GetImportProviders(coreRefData.DataProviders, settings);

            var providerMatched = false;
            foreach (var provider in providers)
            {
                if (settings.ProviderName == null || settings.ProviderName.ToLower() == provider.GetProviderName().ToLower())
                {
                    providerMatched = true;
                    var result = await PerformImport(settings, credentials, coreRefData, provider);

                    Log(result.Log);


                    return result.IsSuccess;

                }
            }

            if (!string.IsNullOrEmpty(settings.ProviderName) && !providerMatched)
            {
                Log($"No import provider was matched for {settings.ProviderName}");
            }

            return false;
        }

        public async Task<List<ChargePoint>> DeDuplicateList(List<ChargePoint> cpList, bool updateDuplicate, CoreReferenceData coreRefData, ImportReport report, bool allowDupeWithDifferentOperator = false, bool fetchExistingFromAPI = false, int dupeDistance = DUPLICATE_DISTANCE_METERS)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var poiManager = new POIManager();

            //get list of all current POIs (in relevant countries) including most delisted ones
            int[] countryIds = (from poi in cpList
                                where poi.AddressInfo.CountryID != null
                                select (int)poi.AddressInfo.CountryID).Distinct().ToArray();

            APIRequestParams filters = new APIRequestParams { CountryIDs = countryIds, MaxResults = 1000000, EnableCaching = true, SubmissionStatusTypeID = new int[] { 0 } };

            IEnumerable<ChargePoint> masterList = new List<ChargePoint>();
            if (fetchExistingFromAPI)
            {
                // fetch from API
                masterList = await _client.GetPOIListAsync(new SearchFilters
                {
                    CountryIDs = countryIds,
                    MaxResults = 1000000,
                    EnableCaching = true,
                    SubmissionStatusTypeIDs = new int[0]
                });
            }
            else
            {
                // use local database

                masterList = await poiManager.GetPOIListAsync(filters);
            }

            /*  var spec = new IndexSpecification<ChargePoint>()

                      .Add(i => i.DataProviderID)
                      .Add(i => i.DataProvidersReference)
                      ;

              var masterList = new IndexSet<ChargePoint>(masterListCollection, spec);
              */
            List<ChargePoint> masterListCopy = new List<ChargePoint>();
            foreach (var tmp in masterList)
            {
                //fully copy of master list item so we have before/after
                masterListCopy.Add(JsonConvert.DeserializeObject<ChargePoint>(JsonConvert.SerializeObject(tmp)));
            }

            //if we failed to get a master list, quit with no result
            if (masterList.Count() == 0) return new List<ChargePoint>();

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

                var itemGeoPos = new GeoCoordinate(item.AddressInfo.Latitude, item.AddressInfo.Longitude);

                //item is duplicate if we already seem to have it based on Data Providers reference or approx position match
                var dupeList = masterList.Where(c =>
                        (
                        // c.DataProvider != null &&
                        c.DataProviderID == item.DataProviderID && c.DataProvidersReference == item.DataProvidersReference)
                        || (c.AddressInfo.Title == item.AddressInfo.Title && c.AddressInfo.AddressLine1 == item.AddressInfo.AddressLine1 && c.AddressInfo.Postcode == item.AddressInfo.Postcode)
                        || (GeoManager.IsClose(c.AddressInfo.Latitude, c.AddressInfo.Longitude, item.AddressInfo.Latitude, item.AddressInfo.Longitude, 2) && new GeoCoordinate(c.AddressInfo.Latitude, c.AddressInfo.Longitude).GetDistanceTo(itemGeoPos) < DUPLICATE_DISTANCE_METERS) //meters distance apart
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

                // hydrate POI with all of its associated extended properties
                var hydratedPoi = UseDataModelComparison ? poiManager.PreviewPopulatedPOIFromModel(poi, coreRefData) : DehydratePOI(poi, coreRefData);

                var differences = poiManager.CheckDifferences(origPOI, hydratedPoi);
                differences.RemoveAll(d => d.Context == ".MetadataValues");
                differences.RemoveAll(d => d.Context == ".DateLastStatusUpdate");
                differences.RemoveAll(d => d.Context == ".DateLastConfirmed");
                differences.RemoveAll(d => d.Context == ".DateLastVerified");
                differences.RemoveAll(d => d.Context == ".UUID");
                differences.RemoveAll(d => d.Context == ".LevelOfDetail");

                differences.RemoveAll(d => d.Context == "DataProvider.DateLastImported");
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
                    ComparisonResult result = compareLogic.Compare(origPOI, hydratedPoi);

                    var diffReport = new KellermanSoftware.CompareNetObjects.Reports.UserFriendlyReport();
                    result.Differences.RemoveAll(d => d.PropertyName == ".MetadataValues");
                    result.Differences.RemoveAll(d => d.PropertyName == ".DateLastStatusUpdate");
                    result.Differences.RemoveAll(d => d.PropertyName == ".UUID");
                    result.Differences.RemoveAll(d => d.PropertyName == "DataProvider.DateLastImported");
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

        /// <summary>
        /// For the given POI, populate extended navigation properties (DataProvider etc) based on current IDs
        /// </summary>
        /// <param name="poi"></param>
        /// <param name="refData"></param>
        /// <returns></returns>
        public ChargePoint DehydratePOI(ChargePoint poi, CoreReferenceData refData)
        {
            poi.DataProvider = refData.DataProviders.FirstOrDefault(i => i.ID == poi.DataProviderID);
            poi.OperatorInfo = refData.Operators.FirstOrDefault(i => i.ID == poi.OperatorID);
            poi.UsageType = refData.UsageTypes.FirstOrDefault(i => i.ID == poi.UsageTypeID);

            foreach (var c in poi.Connections)
            {
                c.ConnectionType = refData.ConnectionTypes.FirstOrDefault(i => i.ID == c.ConnectionTypeID);
                c.CurrentType = refData.CurrentTypes.FirstOrDefault(i => i.ID == c.CurrentTypeID);
                c.Level = refData.ChargerTypes.FirstOrDefault(i => i.ID == c.LevelID);
                c.StatusType = refData.StatusTypes.FirstOrDefault(i => i.ID == c.StatusTypeID);
            }

            return poi;
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
                        bool addressIsSame = (poi.AddressInfo.AddressLine1 == otherPoi.AddressInfo.AddressLine1
                            &&
                            poi.AddressInfo.AddressLine2 == otherPoi.AddressInfo.AddressLine2
                            &&
                            poi.AddressInfo.StateOrProvince == otherPoi.AddressInfo.StateOrProvince
                            );

                        //if address matches or lat/long matches then merge the poi
                        if (addressIsSame || poi.AddressInfo.Latitude == otherPoi.AddressInfo.Latitude && poi.AddressInfo.Longitude == otherPoi.AddressInfo.Longitude)
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
            if (_log != null)
            {
                _log.LogInformation(message);
            }
            else
            {
                this.ImportLog += message + "\r\n";
                System.Diagnostics.Debug.WriteLine(message);
            }

        }

        /// <summary>
        /// Given a list of POIs, returns list of those which are low data quality based on their
        /// content (address etc)
        /// </summary>
        /// <param name="allPOIs">  </param>
        /// <returns>  </returns>
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
        /// <param name="current">  </param>
        /// <param name="previous">  </param>
        /// <returns>  </returns>
        public bool IsDuplicateLocation(ChargePoint current, ChargePoint previous, bool compareTitle)
        {
            //is duplicate item if latlon is exact match for previous item or latlon is within few meters of previous item
            if (
                (GeoManager.IsClose(current.AddressInfo.Latitude, current.AddressInfo.Longitude, previous.AddressInfo.Latitude, previous.AddressInfo.Longitude) && new GeoCoordinate(current.AddressInfo.Latitude, current.AddressInfo.Longitude).GetDistanceTo(new GeoCoordinate(previous.AddressInfo.Latitude, previous.AddressInfo.Longitude)) < DUPLICATE_DISTANCE_METERS) //meters distance apart
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
                    Log(current.AddressInfo.ToString() + " is Duplicate in batch due to exact equal latlon to [Data Provider " + dataProviderId + "]" + previous.AddressInfo.ToString());
                }
                else if (new GeoCoordinate(current.AddressInfo.Latitude, current.AddressInfo.Longitude).GetDistanceTo(new GeoCoordinate(previous.AddressInfo.Latitude, previous.AddressInfo.Longitude)) < DUPLICATE_DISTANCE_METERS)
                {
                    Log(current.AddressInfo.ToString() + " is Duplicate in batch due to close proximity to [Data Provider " + dataProviderId + "]" + previous.AddressInfo.ToString());
                }
                else if (previous.AddressInfo.Title == current.AddressInfo.Title && previous.AddressInfo.AddressLine1 == current.AddressInfo.AddressLine1 && previous.AddressInfo.Postcode == current.AddressInfo.Postcode)
                {
                    Log(current.AddressInfo.ToString() + " is Duplicate in batch due to same Title and matching AddressLine1 and Postcode  [Data Provider " + dataProviderId + "]" + previous.AddressInfo.ToString());
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
                destItem.StatusType = sourceItem.StatusType;
                destItem.StatusTypeID = sourceItem.StatusTypeID;
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

        public async Task<ImportReport> PerformImport(ImportProcessSettings settings, APICredentials credentials, CoreReferenceData coreRefData, IImportProvider provider)
        {
            var p = ((BaseImportProvider)provider);
            p.ExportType = settings.ExportType;

            var outputPath = settings.DefaultDataPath;

            ImportReport resultReport = new ImportReport();
            resultReport.ProviderDetails = p;

            try
            {
                bool loadOK = false;

                if (p is ImportProvider_OCPI ocpi)
                {
                    if (ocpi.CredentialKey != null)
                    {
                        if (settings.Credentials.TryGetValue(ocpi.CredentialKey, out var cred))
                        {
                            ocpi.AuthHeaderValue = cred;
                        }

                    }
                }

                if (p.ImportInitialisationRequired && p is IImportProviderWithInit)
                {
                    ((IImportProviderWithInit)provider).InitImportProvider();
                }

                if (string.IsNullOrEmpty(p.InputPath))
                {
                    p.InputPath = Path.Combine(settings.DefaultDataPath, "cache_" + p.ProviderName + ".dat");
                }

                if (settings.FetchLiveData && p.IsAutoRefreshed && !String.IsNullOrEmpty(p.AutoRefreshURL))
                {
                    Log("Loading input data from URL..");
                    var sw = Stopwatch.StartNew();
                    loadOK = await provider.LoadInputFromURL(p.AutoRefreshURL);
                    sw.Stop();
                    Log($"Data downloaded in {sw.Elapsed.TotalSeconds}s.");
                }
                else
                {
                    if (p.IsStringData && !p.UseCustomReader)
                    {
                        Log("Loading input data from file..");

                        loadOK = await provider.LoadInputFromURL(p.InputPath);
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
                    if (settings.FetchLiveData && settings.CacheInputData)
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

                    if (list.Any(f => f.AddressCleaningRequired == true))
                    {
                        Log("Address cleaning required, performing clean..");
                        await addressLookupCacheManager.LoadCache();
                        // need to perform address lookups
                        foreach (var i in list)
                        {
                            if (i.AddressCleaningRequired == true)
                            {
                                await CleanPOIAddressInfo(i);
                            }
                        }

                        await addressLookupCacheManager.SaveCache();
                    }

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

                    GC.Collect();

                    List<ChargePoint> finalList = new List<ChargePoint>();

                    if (settings.PerformDeduplication)
                    {
                        Log("Deduplicating list (" + p.ProviderName + ":: " + list.Count + " Items)..");

                        //de-duplicate and clean list based on existing data
                        finalList = await DeDuplicateList(list, true, coreRefData, resultReport, p.AllowDuplicatePOIWithDifferentOperator, settings.FetchExistingFromAPI, DUPLICATE_DISTANCE_METERS);
                    }
                    else
                    {
                        //skip deduplication
                        finalList = list.ToList();
                    }


                    Log("Preparing final processed list for output..");

                    // remove items with no changes

                    if (resultReport.Unchanged.Any())
                    {
                        foreach (var r in resultReport.Unchanged)
                        {
                            finalList.Remove(r);
                        }
                    }

                    if (ImportUpdatesOnly)
                    {
                        finalList = finalList.Where(l => l.ID > 0).ToList();
                    }

                    GC.Collect();

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
                        p.ExportJSONFile(finalList, outputPath + "processed_" + p.OutputNamePrefix + ".json");
                    }

                    if (p.ExportType == ExportType.JSONAPI)
                    {
                        Log("Exporting JSON..");
                        //output json
                        var fileName = outputPath + "processed_" + p.OutputNamePrefix + ".json";
                        p.ExportJSONFile(finalList, fileName);

                        Log("Uploading JSON to API..");
                        if (System.IO.File.Exists(fileName))
                        {
                            var json = System.IO.File.ReadAllText(fileName);
                            await UploadPOIList(json);
                        }

                        //  notify API of last date of import for each provider
                        if (p.DataProviderID > 0)
                        {
                            await UpdateLastImportDate(p.DataProviderID);
                        }
                    }

                    if (p.ExportType == ExportType.API && p.IsProductionReady)
                    {
                        //publish list of locations to OCM via API

                        Log("Publishing via API..");
                        foreach (ChargePoint cp in finalList.Where(l => l.AddressInfo.CountryID != null))
                        {
                            _client.UpdatePOI(cp, credentials);
                            if (cp.ID == 0)
                            {
                                numAdded++;
                            }
                            else
                            {
                                numUpdated++;
                            }
                        }

                        //  notify API of last date of import for each provider
                        if (p.DataProviderID > 0)
                        {
                            await UpdateLastImportDate(p.DataProviderID);
                        }

                    }
                    else
                    {
                        numAdded = finalList.Count(p => p.ID == 0);
                        numUpdated = finalList.Count(p => p.ID > 0);
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

        private async Task<ChargePoint> CleanPOIAddressInfo(ChargePoint p)
        {
            var result = await addressLookupCacheManager.PerformLocationLookup(p.AddressInfo.Latitude, p.AddressInfo.Longitude);

            if (result != null && !string.IsNullOrEmpty(result.AddressResult.AddressLine1))
            {
                p.AddressInfo.Title = result.AddressResult.Title;
                p.AddressInfo.AddressLine1 = result.AddressResult.AddressLine1;

                p.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Imported_Published;
                return p;
            }
            else
            {
                // address cannot be improved and is low quality
                if (!string.IsNullOrEmpty(p.AddressInfo.Postcode))
                {
                    p.AddressInfo.Title = p.AddressInfo.Postcode;
                    p.AddressInfo.AddressLine1 = p.AddressInfo.Town;
                }
                return p;
            }

        }

        public async Task<string> UploadPOIList(string json)
        {
            var poiList = JsonConvert.DeserializeObject<List<ChargePoint>>(json);

            Log("Publishing via API..");

            var itemCount = poiList.Count();
            var itemsProcessed = 0;
            var pageSize = 100;
            var pageIndex = 0;

            Log($"Publishing a total of {itemCount} items ..");

            while (itemsProcessed < itemCount)
            {
                IEnumerable<ChargePoint> subList = new List<ChargePoint>();
                var currentIndex = pageIndex * pageSize;
                var itemsRemaining = itemCount - currentIndex;

                if (itemsRemaining > pageSize)
                {
                    subList = poiList.Skip(currentIndex).Take(pageSize);
                    itemsProcessed += pageSize;
                    Log($"[{DateTime.Now}] Publishing items {currentIndex} to {currentIndex + pageSize - 1}..");
                }
                else
                {
                    subList = poiList.Skip(currentIndex).Take(itemsRemaining);
                    itemsProcessed += itemsRemaining;
                    Log($"[{DateTime.Now}] Publishing items {currentIndex} to {currentIndex + itemsRemaining}..");
                }

                _client.UpdateItems(subList.ToList());

                await Task.Delay(500);
                pageIndex++;
            }

            return $"Added: {poiList.Count(i => i.ID == 0)} Updated: {poiList.Count(i => i.ID > 0)}";
        }

        /// <summary>
        /// Tell API we have completed our import for this provider
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns></returns>
        public async Task UpdateLastImportDate(int providerId)
        {
            await _client.Get("/system/importcompleted/" + providerId);
        }

        /// <summary>
        /// For a list of ChargePoint objects, attempt to populate the AddressInfo (at least country)
        /// based on lat/lon if not already populated
        /// </summary>
        /// <param name="itemList">  </param>
        /// <param name="coreRefData">  </param>

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

        public List<ChargePoint> PopulateLocationFromGeolocationCache(IEnumerable<ChargePoint> itemList, CoreReferenceData coreRefData)
        {
            //OCM.Import.Analysis.SpatialAnalysis spatialAnalysis = new Analysis.SpatialAnalysis(_settings.GeolocationShapefilePath + "/ne_10m_admin_0_map_units.shp");
            var spatialAnalysis = new Analysis.SpatialAnalysis(Path.Join(_settings.GeolocationShapefilePath, "ne_10m_admin_0_countries.shp"));

            List<ChargePoint> failedLookups = new List<ChargePoint>();

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
                        LogHelper.Log("Failed to resolve country for item:" + item.AddressInfo.Title + " OCM-" + item.ID);

                        failedLookups.Add(item);
                    }
                    else
                    {
                        item.AddressInfo.CountryID = item.AddressInfo.Country.ID;
                    }
                }
            }

            //cache may have updates, save for next time
            geolocationCacheManager.SaveCache();
            return failedLookups;
        }

        public APICredentials GetAPISessionCredentials(string identifier, string sessionToken)
        {
            return new APICredentials { Identifier = identifier, SessionToken = sessionToken };
        }

        public async Task GeocodingTestCountries()
        {

            //get a few OCM listings and check that their country appears to be correct
            // curl  "https://api.openchargemap.io/v3/poi?key=test&maxresults=20000000" --output  C:\Temp\ocm\data\import\poi.json
            var cachePath = @"C:\temp\ocm\data\import\poi.json";

            List<ChargePoint> poiList;
            var coreRefData = await _client.GetCoreReferenceDataAsync();

            if (!File.Exists(cachePath))
            {
                var filters = new SearchFilters { SubmissionStatusTypeIDs = new int[] { (int)StandardSubmissionStatusTypes.Imported_Published, (int)StandardSubmissionStatusTypes.Submitted_Published }, MaxResults = 200000, EnableCaching = true };

                poiList = (await _client.GetPOIListAsync(filters)).ToList();

                await System.IO.File.WriteAllTextAsync(cachePath, JsonConvert.SerializeObject(poiList));
            }
            else
            {

                var list = new List<ChargePoint>();

                JsonSerializer serializer = new JsonSerializer();

                using (FileStream st = File.Open(cachePath, FileMode.Open))
                using (StreamReader sr = new StreamReader(st))
                using (JsonReader reader = new JsonTextReader(sr))
                {

                    while (reader.Read())
                    {
                        // deserialize only when there's "{" character in the stream
                        if (reader.TokenType == JsonToken.StartObject)
                        {
                            var o = serializer.Deserialize<ChargePoint>(reader);
                            list.Add(o);
                        }
                    }
                }

                poiList = list.Where(p => p.AddressInfo.CountryID != 159 && p.AddressInfo.CountryID != 1).ToList();
            }

            // if some locations are known to fail lookup, don't attempt them
            var knownFailsFile = @"C:\temp\ocm\data\import\failed-country-lookups.json";
            List<ChargePoint> knownFails = new List<ChargePoint>();
            if (File.Exists(knownFailsFile))
            {
                knownFails = JsonConvert.DeserializeObject<List<ChargePoint>>(await File.ReadAllTextAsync(knownFailsFile));
                var list = (List<ChargePoint>)poiList;
                foreach (var p in knownFails)
                {
                    list.Remove(list.FirstOrDefault(l => l.ID == p.ID));
                }
            }

            var poiListCopy = JsonConvert.DeserializeObject<List<ChargePoint>>(JsonConvert.SerializeObject(poiList));

            // clear existing country info
            foreach (var p in poiList)
            {
                p.AddressInfo.Country = null;
                p.AddressInfo.CountryID = null;
            }

            // determine country
            var s = Stopwatch.StartNew();
            var failedLookups = PopulateLocationFromGeolocationCache(poiList, coreRefData);
            s.Stop();
            System.Diagnostics.Debug.WriteLine("Lookup took " + s.Elapsed.TotalSeconds + "s");

            // log failed lookups
            foreach (var p in failedLookups)
            {
                if (!knownFails.Any(k => k.ID == p.ID))
                {
                    knownFails.Add(p);
                }
            }
            await System.IO.File.WriteAllTextAsync(knownFailsFile, JsonConvert.SerializeObject(knownFails, Formatting.Indented));


            var file = @"C:\temp\ocm\data\import\country-fixes.csv";

            using (var stream = File.CreateText(file))
            {
                stream.WriteLine($"Id,AddressInfoId,OriginalCountryId,NewCountryId");
                foreach (var p in poiList)
                {
                    var orig = poiListCopy.Find(f => f.ID == p.ID);

                    if (p.AddressInfo.CountryID == null)
                    {
                        // could not check, use original
                        p.AddressInfo.Country = orig.AddressInfo.Country;
                        p.AddressInfo.CountryID = orig.AddressInfo.CountryID;
                    }

                    if (orig.AddressInfo.CountryID != p.AddressInfo.CountryID)
                    {

                        stream.WriteLine($"{p.ID},{p.AddressInfo.ID},{orig.AddressInfo.CountryID}, {p.AddressInfo.CountryID}");
                    }
                }
                stream.Flush();
                stream.Close();
            }

            var changedCountries = 0;
            foreach (var p in poiList)
            {
                var orig = poiListCopy.Find(f => f.ID == p.ID);

                if (orig.AddressInfo.CountryID != p.AddressInfo.CountryID)
                {
                    changedCountries++;
                    System.Diagnostics.Debug.WriteLine("OCM-" + p.ID + " country changed to " + p.AddressInfo.CountryID + " from " + orig.AddressInfo.CountryID);
                }

            }
            System.Diagnostics.Debug.WriteLine("Total: " + changedCountries + " changed of " + poiList.Count());


        }

        #endregion helper methods
    }
}