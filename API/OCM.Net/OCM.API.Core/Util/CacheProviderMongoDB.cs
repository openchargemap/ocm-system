using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.API.Common.Model.Extended;
using OCM.Core.Settings;

namespace OCM.Core.Data
{
    public class MirrorStatus
    {
        public HttpStatusCode StatusCode { get; set; }

        public string Description { get; set; }

        public long TotalPOIInCache { get; set; }

        public long TotalPOIInDB { get; set; }

        public DateTime LastUpdated { get; set; }

        public long NumPOILastUpdated { get; set; }

        public long NumDistinctPOIs { get; set; }
        public long MaxBatchSize { get; set; }

        public string Server { get; set; }

        public string ContentHash { get; set; }

        public DateTime? LastPOIUpdate { get; set; }
        public DateTime? LastPOICreated { get; set; }
        public int MaxPOIId { get; set; }
    }

    public class BenchmarkResult
    {
        public string Description { get; set; }
        public long TimeMS { get; set; }
    }

    public class POIMongoDB : OCM.API.Common.Model.ChargePoint
    {
        [JsonIgnore]
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> SpatialPosition { get; set; }

        public static POIMongoDB FromChargePoint(OCM.API.Common.Model.ChargePoint cp, POIMongoDB poi = null)
        {
            if (poi == null) poi = new POIMongoDB();

            poi.AddressInfo = cp.AddressInfo;
            poi.Chargers = cp.Chargers;
            poi.Connections = cp.Connections;
            poi.DataProvider = cp.DataProvider;
            poi.DataProviderID = cp.DataProviderID;
            poi.DataProvidersReference = cp.DataProvidersReference;
            poi.DataQualityLevel = cp.DataQualityLevel;

            if (cp.DateCreated != null)
            {
                poi.DateCreated = (DateTime?)DateTime.SpecifyKind(cp.DateCreated.Value, DateTimeKind.Utc);
            }
            if (cp.DateLastConfirmed != null)
            {
                poi.DateLastConfirmed = (DateTime?)DateTime.SpecifyKind(cp.DateLastConfirmed.Value, DateTimeKind.Utc);
            }
            if (cp.DateLastStatusUpdate != null)
            {
                poi.DateLastStatusUpdate = (DateTime?)DateTime.SpecifyKind(cp.DateLastStatusUpdate.Value, DateTimeKind.Utc);
            }
            if (cp.DatePlanned != null)
            {
                poi.DatePlanned = (DateTime?)DateTime.SpecifyKind(cp.DatePlanned.Value, DateTimeKind.Utc);
            }

            poi.GeneralComments = cp.GeneralComments;
            poi.ID = cp.ID;
            poi.MediaItems = cp.MediaItems;
            poi.MetadataTags = cp.MetadataTags;
            poi.MetadataValues = cp.MetadataValues;
            poi.NumberOfPoints = cp.NumberOfPoints;
            poi.NumberOfPoints = cp.NumberOfPoints;
            poi.OperatorID = cp.OperatorID;
            poi.OperatorInfo = cp.OperatorInfo;
            poi.OperatorsReference = cp.OperatorsReference;
            poi.ParentChargePointID = cp.ParentChargePointID;
            poi.StatusType = cp.StatusType;
            poi.StatusTypeID = cp.StatusTypeID;
            poi.SubmissionStatus = cp.SubmissionStatus;
            poi.SubmissionStatusTypeID = cp.SubmissionStatusTypeID;
            poi.UsageCost = cp.UsageCost;
            poi.UsageType = cp.UsageType;
            poi.UsageTypeID = cp.UsageTypeID;
            poi.UserComments = cp.UserComments;
            poi.LevelOfDetail = cp.LevelOfDetail;
            poi.UUID = cp.UUID;
            return poi;
        }
    }

    public class CacheProviderMongoDB
    {
        private const int DefaultPolylineSearchDistanceKM = 5;
        private const int DefaultLatLngSearchDistanceKM = 1000;
        private MongoDatabase database = null;
        private MongoClient client = null;
        private MongoServer server = null;
        private MirrorStatus status = null;

        private static readonly object _mutex = new object();
        private static volatile CacheProviderMongoDB _instance = null;
        private static CoreSettings _settings;

        public static CacheProviderMongoDB CreateDefaultInstance(CoreSettings settings)
        {
            _settings = settings;
            return DefaultInstance;
        }

        public static bool IsDefaultInstanceInitialized
        {
            get
            {
                if (_settings == null || _instance == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        public static CacheProviderMongoDB DefaultInstance
        {
            get
            {

                if (_instance == null)
                {
                    lock (_mutex)
                    {
                        if (_instance == null)
                        {
                            if (_settings == null) throw new Exception("Cache Provider Default Instance requires init using CreateDefaultInstance");

                            _instance = new CacheProviderMongoDB();
                        }
                    }
                }

                return _instance;
            }
        }

        public CacheProviderMongoDB()
        {
            try
            {
                if (!BsonClassMap.IsClassMapRegistered(typeof(POIMongoDB)))
                {
                    // register deserialization class map for ChargePoint
                    BsonClassMap.RegisterClassMap<POIMongoDB>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true);
                    });
                }

                if (!BsonClassMap.IsClassMapRegistered(typeof(OCM.API.Common.Model.ChargePoint)))
                {
                    // register deserialization class map for ChargePoint
                    BsonClassMap.RegisterClassMap<OCM.API.Common.Model.ChargePoint>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true);
                    });
                }
                if (!BsonClassMap.IsClassMapRegistered(typeof(OCM.API.Common.Model.CoreReferenceData)))
                {
                    // register deserialization class map for ChargePoint
                    BsonClassMap.RegisterClassMap<OCM.API.Common.Model.CoreReferenceData>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true);
                    });
                }

                if (!BsonClassMap.IsClassMapRegistered(typeof(MirrorStatus)))
                {
                    // register deserialization class map for ChargePoint
                    BsonClassMap.RegisterClassMap<MirrorStatus>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true);
                    });
                }

                if (!BsonClassMap.IsClassMapRegistered(typeof(CountryExtendedInfo)))
                {
                    // register deserialization class map for ChargePoint
                    BsonClassMap.RegisterClassMap<CountryExtendedInfo>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true);
                    });
                }
            }
            catch (Exception)
            {
                ; ;
            }

            client = new MongoClient(_settings.MongoDBSettings.ConnectionString);
            server = client.GetServer();
            database = server.GetDatabase(_settings.MongoDBSettings.DatabaseName);
            status = GetMirrorStatus(false, false);

            if (status.StatusCode == HttpStatusCode.ExpectationFailed && _settings.IsCacheOnlyMode)
            {
                throw new Exception("MongoDB Cache Unavailable. Check configuration settings for connection string. ::" + status.Description);
            }
        }

        public MongoCollection<POIMongoDB> GetPOICollection()
        {
            return database.GetCollection<POIMongoDB>("poi");
        }

        public void RemoveAllPOI(IEnumerable<OCM.API.Common.Model.ChargePoint> poiList, MongoCollection<POIMongoDB> poiCollection)
        {
            foreach (var poi in poiList)
            {
                var query = Query.EQ("ID", poi.ID);
                poiCollection.Remove(query);
            }
        }

        public void InsertAllPOI(IEnumerable<OCM.API.Common.Model.ChargePoint> poiList, MongoCollection<POIMongoDB> poiCollection)
        {
            var mongoDBPoiList = new List<POIMongoDB>();
            foreach (var poi in poiList)
            {
                var newPoi = POIMongoDB.FromChargePoint(poi);
                if (newPoi.AddressInfo != null)
                {
                    newPoi.SpatialPosition = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(newPoi.AddressInfo.Longitude, newPoi.AddressInfo.Latitude));
                }
                mongoDBPoiList.Add(newPoi);
            }
            poiCollection.InsertBatch(mongoDBPoiList);
        }

        public async Task<List<OCM.API.Common.Model.ChargePoint>> GetPOIListToUpdate(OCMEntities dataModel, CacheUpdateStrategy updateStrategy, CoreReferenceData refData, int pageIndex = 0, int pageSize = 0)
        {
            if (!database.CollectionExists("poi"))
            {
                database.CreateCollection("poi");
            }

            IQueryable<Data.ChargePoint> dataList = dataModel.ChargePoints
                 .Include(a1 => a1.AddressInfo)
                      .ThenInclude(a => a.Country)
                 .Include(a1 => a1.ConnectionInfoes)
                 .Include(a1 => a1.MetadataValues)
                      .ThenInclude(m => m.MetadataFieldOption)
                 .Include(a1 => a1.UserComments)
                      .ThenInclude(c => c.User)
                  .Include(a1 => a1.UserComments)
                 .Include(a1 => a1.MediaItems)
                    .ThenInclude(c => c.User)
                 .OrderBy(o => o.Id);

            if (pageSize > 0)
            {
                dataList = dataList.Skip(pageIndex * pageSize).Take(pageSize);
            }

            dataList = dataList.AsNoTracking();

            //incremental update based on POI Id - up to 100 results at a time
            if (updateStrategy == CacheUpdateStrategy.Incremental)
            {
                var poiCollection = database.GetCollection<POIMongoDB>("poi");
                var maxPOI = poiCollection.FindAll().SetSortOrder(SortBy.Descending("ID")).SetLimit(1).FirstOrDefault();
                int maxId = 0;
                if (maxPOI != null) { maxId = maxPOI.ID; }

                //from max poi we have in mirror to next 100 results in order of ID
                dataList = dataList.Where(o => o.Id > maxId).Take(100);

                var poiList = new List<OCM.API.Common.Model.ChargePoint>();

                foreach (var cp in dataList)
                {
                    poiList.Add(OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(cp, true, true, true, true, refData));
                }
                return poiList;
            }



            //update based on POI last modified since last status update
            if (updateStrategy == CacheUpdateStrategy.Modified)
            {
                var poiCollection = database.GetCollection<POIMongoDB>("poi");
                var maxPOI = poiCollection.FindAll().SetSortOrder(SortBy.Descending("DateLastStatusUpdate")).SetLimit(1).FirstOrDefault();

                DateTime? dateLastModified = null;
                if (maxPOI != null) { dateLastModified = maxPOI.DateLastStatusUpdate.Value.AddMinutes(-10); }

                //determine POI updated since last status update we have in cache
                var stopwatch = Stopwatch.StartNew();

                dataList = dataList.Where(o => o.DateLastStatusUpdate > dateLastModified);

                stopwatch.Stop();

                System.Diagnostics.Debug.WriteLine($"POI List retrieved in {stopwatch.Elapsed.TotalSeconds } seconds");

                var poiList = new List<OCM.API.Common.Model.ChargePoint>();
                stopwatch.Restart();

                foreach (var cp in dataList)
                {
                    poiList.Add(OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(cp, true, true, true, true, refData));
                }

                System.Diagnostics.Debug.WriteLine($"POI List model prepared in {stopwatch.Elapsed.TotalSeconds } seconds");

                return poiList;
            }

            if (updateStrategy == CacheUpdateStrategy.All)
            {
                var stopwatch = Stopwatch.StartNew();

                // get data list, include navigation properties to improve query performance
                var list = await dataList.ToListAsync();

                stopwatch.Stop();

                System.Diagnostics.Debug.WriteLine($"POI List retrieved in {stopwatch.Elapsed.TotalSeconds } seconds");
                stopwatch.Restart();

                var poiList = new List<OCM.API.Common.Model.ChargePoint>();

                foreach (var cp in list)
                {
                    poiList.Add(OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(cp, true, true, true, true, refData));

                    if (poiList.Count % 100 == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"POIs processed { poiList.Count} in {stopwatch.Elapsed.TotalSeconds } seconds");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"POI List model prepared in {stopwatch.Elapsed.TotalSeconds } seconds");

                return poiList;
            }

            return null;

        }

        public async Task<MirrorStatus> RefreshCachedPOI(int poiId)
        {
            var mirrorStatus = await Task.Run<MirrorStatus>(() =>
            {

                var refData = new ReferenceDataManager().GetCoreReferenceData();
                var dataModel = new OCMEntities();
                var poiModel = dataModel.ChargePoints.FirstOrDefault(p => p.Id == poiId);

                var poiCollection = database.GetCollection<POIMongoDB>("poi");
                var query = Query.EQ("ID", poiId);
                var removeResult = poiCollection.Remove(query);
                System.Diagnostics.Debug.WriteLine("POIs removed from cache [" + poiId + "]:" + removeResult.DocumentsAffected);

                if (poiModel != null)
                {
                    var cachePOI = POIMongoDB.FromChargePoint(OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(poiModel, refData));
                    if (cachePOI.AddressInfo != null)
                    {
                        cachePOI.SpatialPosition = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(cachePOI.AddressInfo.Longitude, cachePOI.AddressInfo.Latitude));
                    }

                    poiCollection.Insert<POIMongoDB>(cachePOI);
                }
                else
                {
                    //poi not present in master DB, we've removed it from cache
                }

                long numPOIInMasterDB = dataModel.ChargePoints.LongCount();
                return RefreshMirrorStatus(poiCollection.Count(), 1, numPOIInMasterDB);
            });

            return mirrorStatus;
        }

        /// <summary>
        /// Ensure all MongoDB indexes are being set up.
        /// </summary>
        /// <returns></returns>
        protected void ensureMongoDBIndexes() {
            var poiCollection = database.GetCollection<POIMongoDB>("poi");
            poiCollection.CreateIndex(IndexKeys.GeoSpatial("SpatialPosition.coordinates"));
            poiCollection.CreateIndex(IndexKeys<POIMongoDB>.Descending(x => x.DateLastStatusUpdate));
            poiCollection.CreateIndex(IndexKeys<POIMongoDB>.Descending(x => x.DateCreated));
            poiCollection.CreateIndex(IndexKeys<POIMongoDB>.Descending(x => x.ID));
        }

        /// <summary>
        /// Perform full or partial repopulation of POI Mirror in MongoDB
        /// </summary>
        /// <returns></returns>
        public async Task<MirrorStatus> PopulatePOIMirror(CacheUpdateStrategy updateStrategy, ILogger logger = null)
        {
            bool preserveExistingPOIs = true;

            ensureMongoDBIndexes();

            // cache will refresh either from the source database or via a master API
            if (!_settings.IsCacheOnlyMode)
            {
                using (var dataModel = new Data.OCMEntities())
                {

                    if (!database.CollectionExists("poi"))
                    {
                        database.CreateCollection("poi");
                    }

                    if (updateStrategy != CacheUpdateStrategy.All)
                    {
                        if (!database.CollectionExists("reference"))
                        {
                            database.CreateCollection("reference");
                        }
                        if (!database.CollectionExists("status"))
                        {
                            database.CreateCollection("status");
                        }
                        if (!database.CollectionExists("poi"))
                        {
                            database.CreateCollection("poi");
                        }
                    }
                    else
                    {
                        // by default we remove all POIs before refreshing from master
                        preserveExistingPOIs = false;
                    }

                    CoreReferenceData coreRefData;

                    using (var refDataManager = new ReferenceDataManager())
                    {
                        coreRefData = refDataManager.GetCoreReferenceData(new APIRequestParams { AllowDataStoreDB = true, AllowMirrorDB = false });
                    }

                    if (coreRefData != null)
                    {
                        database.DropCollection("reference");
                        //problems clearing data from collection...

                        var reference = database.GetCollection<CoreReferenceData>("reference");
                        var query = new QueryDocument();
                        reference.Remove(query, RemoveFlags.None);

                        await Task.Delay(300);
                        reference.Insert(coreRefData);
                    }

                    var batchSize = 1000;

                    List<API.Common.Model.ChargePoint> poiList;

                    var poiCollection = database.GetCollection<POIMongoDB>("poi");
                    if (updateStrategy != CacheUpdateStrategy.All)
                    {
                        poiList = await GetPOIListToUpdate(dataModel, updateStrategy, coreRefData);


                        if (poiList != null && poiList.Any())
                        {

                            RemoveAllPOI(poiList, poiCollection);

                            await Task.Delay(300);
                            InsertAllPOI(poiList, poiCollection);
                        }
                    }
                    else
                    {
                        // full refresh

                        poiCollection.RemoveAll();

                        await Task.Delay(300);

                        var pageIndex = 0;
                        var pageSize = 1000;
                        var total = 0;

                        //get batch of poi to insert
                        var sw = Stopwatch.StartNew();
                        poiList = await GetPOIListToUpdate(dataModel, updateStrategy, coreRefData, pageIndex, pageSize);
                        while (poiList.Count > 0)
                        {
                            logger?.LogInformation($"Inserting batch {pageIndex}");

                            System.Diagnostics.Debug.WriteLine($"Inserting batch {pageIndex} :: {pageIndex * pageSize}");

                            InsertAllPOI(poiList, poiCollection);

                            pageIndex++;
                            total += poiList.Count;

                            poiList = await GetPOIListToUpdate(dataModel, updateStrategy, coreRefData, pageIndex, pageSize);
                        }

                        poiCollection.ReIndex();

                        sw.Stop();
                        GC.Collect();
                        System.Diagnostics.Debug.WriteLine($"Rebuild of complete cache took {sw.Elapsed.TotalSeconds}s");

                    }

                    long numPOIInMasterDB = dataModel.ChargePoints.LongCount();
                    return RefreshMirrorStatus(poiCollection.Count(), 1, numPOIInMasterDB);
                }
            }
            else
            {
                // cache must refresh from master API

                var baseUrl = _settings.DataSourceAPIBaseUrl;
                using (var apiClient = new OCM.API.Client.OCMClient(baseUrl, _settings.ApiKeys.OCMApiKey, logger))
                {
                    // check sync status compared to master API
                    var syncStatus = await apiClient.GetSystemStatusAsync();

                    if (syncStatus != null)
                    {
                        var poiCollection = database.GetCollection<POIMongoDB>("poi");

                        bool isRefDataSyncRequired = false;
                        DateTime? lastUpdated = null;
                        DateTime? lastCreated = null;
                        int maxIdCached = 0;

                        if (poiCollection.Count() == 0)
                        {
                            // no data, starting a new mirror
                            updateStrategy = CacheUpdateStrategy.All;
                            isRefDataSyncRequired = true;
                        }
                        else
                        {
                            var queryablePOICollection = poiCollection.AsQueryable();
                            lastUpdated = queryablePOICollection.Max(i => i.DateLastStatusUpdate);
                            lastCreated = queryablePOICollection.Max(i => i.DateCreated);
                            maxIdCached = queryablePOICollection.Max(i => i.ID);

                            if (maxIdCached < syncStatus.MaxPOIId)
                            {
                                // if our max id is less than the master, we still have some catching up to do so sync on ID first
                                updateStrategy = CacheUpdateStrategy.All;
                                preserveExistingPOIs = true;
                            }

                            // existing data, sync may be required
                            var hashItems = syncStatus.DataHash.Split(";");
                            var hashChecks = syncStatus.DataHash.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                               .Select(part => part.Split("::"))
                                               .ToDictionary(split => split[0], split => split[1]);

                            var localHash = GetCacheContentHash();

                            var localHashChecks = localHash.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(part => part.Split("::"))
                                          .ToDictionary(split => split[0], split => split[1]);

                            if (!hashChecks.ContainsKey("reference") || !localHashChecks.ContainsKey("reference"))
                            {
                                isRefDataSyncRequired = true;
                            }
                            else if (hashChecks["reference"] != localHashChecks["reference"])
                            {
                                isRefDataSyncRequired = true;
                            }
                        }

                        // updates are required to reference data or POI list
                        if (isRefDataSyncRequired || lastUpdated != syncStatus.POIDataLastModified || lastCreated != syncStatus.POIDataLastCreated || updateStrategy == CacheUpdateStrategy.All)
                        {
                            var numPOIUpdated = 0L;

                            var dateLastSync = syncStatus.POIDataLastModified;

                            if (isRefDataSyncRequired)
                            {
                                // sync reference data
                                CoreReferenceData coreRefData = await apiClient.GetCoreReferenceDataAsync();
                                if (coreRefData != null)
                                {
                                    database.DropCollection("reference");
                                    //problems clearing data from collection...

                                    var reference = database.GetCollection<CoreReferenceData>("reference");
                                    var query = new QueryDocument();
                                    reference.Remove(query, RemoveFlags.None);

                                    Thread.Sleep(300);
                                    reference.Insert(coreRefData);
                                }

                            }

                            if (lastUpdated != syncStatus.POIDataLastModified || lastCreated != syncStatus.POIDataLastCreated || updateStrategy == CacheUpdateStrategy.All)
                            {

                                var poiFilter = new API.Client.SearchFilters
                                {
                                    MaxResults = _settings.MongoDBSettings.CacheSyncBatchSize,
                                    ModifiedSince = lastUpdated,
                                    SortBy = "modified_asc",
                                    IncludeUserComments = true,
                                    Verbose = true,
                                    SubmissionStatusTypeIDs = new int[] { 0 }
                                };

                                if (updateStrategy != CacheUpdateStrategy.All)
                                {
                                    if (lastCreated < lastUpdated)
                                    {
                                        poiFilter.ModifiedSince = lastCreated;
                                    }
                                }
                                else
                                {
                                    poiFilter.ModifiedSince = null;
                                    poiFilter.SortBy = "id_asc";
                                    poiFilter.GreaterThanId = maxIdCached;
                                }

                                // sync POI list
                                var poiList = await apiClient.GetPOIListAsync(poiFilter);


                                if (poiList != null && poiList.Any())
                                {
                                    if (!preserveExistingPOIs)
                                    {
                                        if (updateStrategy == CacheUpdateStrategy.All)
                                        {
                                            poiCollection.RemoveAll();
                                        }
                                        else
                                        {
                                            RemoveAllPOI(poiList, poiCollection);
                                        }
                                    }

                                    Thread.Sleep(300);
                                    InsertAllPOI(poiList, poiCollection);

                                    if (updateStrategy == CacheUpdateStrategy.All)
                                    {
                                        poiCollection.ReIndex();
                                    }

                                    numPOIUpdated = poiList.LongCount();
                                }

                            }

                            var status = RefreshMirrorStatus(poiCollection.Count(), numPOIUpdated, poiCollection.Count(), dateLastSync);
                            status.MaxBatchSize = _settings.MongoDBSettings.CacheSyncBatchSize;
                            return status;

                        }
                    }
                }
            }

            // nothing to update
            return GetMirrorStatus(false, false, false);
        }

        private MirrorStatus RefreshMirrorStatus(
            long cachePOICollectionCount, long numPOIUpdated, long numPOIInMasterDB,
            DateTime? poiLastUpdate = null,
            DateTime? poiLastCreated = null
            )
        {
            var statusCollection = database.GetCollection<MirrorStatus>("status");
            statusCollection.Drop();

            //new status
            MirrorStatus status = new MirrorStatus();

            status.Description = "MongoDB Cache of Open Charge Map POI Database";
            status.LastUpdated = DateTime.UtcNow;

            status.TotalPOIInCache = cachePOICollectionCount;
            status.TotalPOIInDB = numPOIInMasterDB;
            status.StatusCode = HttpStatusCode.OK;
            status.NumPOILastUpdated = numPOIUpdated;

            if (poiLastUpdate != null) status.LastPOIUpdate = poiLastUpdate;
            if (poiLastCreated != null) status.LastPOICreated = poiLastCreated;

            statusCollection.Insert(status);
            return status;
        }

        private string GetCacheContentHash()
        {
            var cols = new string[] { "poi", "reference", "countryinfo" };

            var hashCommand = new CommandDocument {
                    { "dbHash", 1 },
                    { "collections", BsonArray.Create(cols) }
                };

            var dbHashResults = database.RunCommand(hashCommand);
            if (dbHashResults.Ok)
            {
                var collectionHashes = dbHashResults.Response.GetElement("collections").Value.AsBsonDocument.Elements
                    .OrderBy(i => i.Name)
                    .Select(i => i.Name + "::" + i.Value.AsString)
                    .ToArray();
                return String.Join(";", collectionHashes);
            }
            else
            {
                return null;
            }
        }

        public MirrorStatus GetMirrorStatus(bool includeDupeCheck, bool includeDBCheck = true, bool includeContentHash = false)
        {
            try
            {
                var statusCollection = database.GetCollection<MirrorStatus>("status");
                var currentStatus = statusCollection.FindOne();

                if (includeContentHash)
                {
                    currentStatus.ContentHash = GetCacheContentHash();
                }

                if (includeDBCheck)
                {
                    using (var db = new OCMEntities())
                    {
                        currentStatus.TotalPOIInDB = db.ChargePoints.LongCount();
                        currentStatus.LastPOIUpdate = db.ChargePoints.Max(i => i.DateLastStatusUpdate);
                        currentStatus.LastPOICreated = db.ChargePoints.Max(i => i.DateCreated);
                        currentStatus.MaxPOIId = db.ChargePoints.Max(i => i.Id);
                    }
                }

                if (includeDupeCheck)
                {
                    //perform check to see if number of distinct ids!= number of pois
                    var poiCollection = database.GetCollection<POIMongoDB>("poi");
                    var distinctPOI = poiCollection.Distinct("ID");
                    currentStatus.NumDistinctPOIs = distinctPOI.Count();
                }

                currentStatus.Server = server.Settings.Server.Host + ":" + server.Settings.Server.Port;
                return currentStatus;
            }
            catch (Exception exp)
            {
                return new MirrorStatus { StatusCode = HttpStatusCode.NotFound, Description = "Cache is offline:" + exp.ToString() };
            }
        }

        public List<CountryExtendedInfo> GetExtendedCountryInfo()
        {
            try
            {
                var results = database.GetCollection<CountryExtendedInfo>("countryinfo");
                return results.FindAll().ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool IsCacheReady()
        {
            if (status != null && (_settings.MongoDBSettings.MaxCacheAgeMinutes == 0 || status.LastUpdated.AddMinutes(_settings.MongoDBSettings.MaxCacheAgeMinutes) > DateTime.UtcNow))
            {
                return true;
            }
            else
            {
                status = GetMirrorStatus(false, false);
                return false;
            }
        }
        public CoreReferenceData GetCoreReferenceData(APIRequestParams filter)
        {
            try
            {
                if (IsCacheReady())
                {
                    var refDataCollection = database.GetCollection<CoreReferenceData>("reference");
                    var refData = refDataCollection.FindOne();


                    if (filter.CountryIDs != null && filter.CountryIDs.Any())
                    {
                        // need to filter results based on usage by country
                        var poiCollection = database.GetCollection<POIMongoDB>("poi");

                        var connectionsInCountry = poiCollection.AsQueryable().AsNoTracking().Where(poi =>
                            poi.AddressInfo.CountryID != null
                            && filter.CountryIDs.Contains((int)poi.AddressInfo.CountryID)
                            && (poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Imported_Published || poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Submitted_Published)
                            && poi.Connections.Any()
                        )
                            .Select(p => new { CountryID = p.AddressInfo.CountryID, ConnectionTypes = p.Connections.Select(t => t.ConnectionTypeID).AsEnumerable() })
                            .ToArray()
                            .SelectMany(p => p.ConnectionTypes, (i, c) => new { CountryId = i.CountryID, ConnectionTypeId = c })
                            .Distinct();

                        refData.ConnectionTypes.RemoveAll(a => !connectionsInCountry.Any(r => r.ConnectionTypeId == a.ID));


                        // filter on operators present within given countries

                        var operatorsInCountry = poiCollection.AsQueryable().AsNoTracking()
                            .Where(poi =>
                               poi.AddressInfo.CountryID != null
                               && filter.CountryIDs.Contains((int)poi.AddressInfo.CountryID)
                               && (poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Imported_Published || poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Submitted_Published)
                               && poi.OperatorID != null)
                             .Select(p => new { CountryId = p.AddressInfo.CountryID, OperatorId = p.OperatorID })
                             .ToArray()
                             .Distinct();

                        refData.Operators.RemoveAll(a => !operatorsInCountry.Any(r => r.OperatorId == a.ID));

                    }

                    return refData;
                }
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.ToString());
                ; ;
            }

            return null;
        }

        /// <summary>
        /// returns cached POI details if cache is not older than ageMinutes
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public OCM.API.Common.Model.ChargePoint GetPOI(int id)
        {
            if (IsCacheReady())
            {
                var poiCollection = database.GetCollection<OCM.API.Common.Model.ChargePoint>("poi").AsQueryable();
                return poiCollection.FirstOrDefault(p => p.ID == id);
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<OCM.API.Common.Model.ChargePoint> GetPOIList(APIRequestParams settings)
        {
            return GetPOIListAsync(settings).Result;
        }

        public async Task<IEnumerable<OCM.API.Common.Model.ChargePoint>> GetPOIListAsync(APIRequestParams filter)
        {

            if (!IsCacheReady())
            {
                System.Diagnostics.Debug.Print("MongoDB cache is outdated, returning null result.");
                return null;
            }

            var stopwatch = Stopwatch.StartNew();

            int maxResults = filter.MaxResults;

            bool requiresDistance = false;
            GeoJsonPoint<GeoJson2DGeographicCoordinates> searchPoint = null;

            if (filter.Latitude != null && filter.Longitude != null)
            {
                requiresDistance = true;
                searchPoint = GeoJson.Point(GeoJson.Geographic((double)filter.Longitude, (double)filter.Latitude));
            }
            else
            {
                searchPoint = GeoJson.Point(GeoJson.Geographic(0, 0));
            }

            //if distance filter provided in miles, convert to KM before use

            if (filter.DistanceUnit == OCM.API.Common.Model.DistanceUnit.Miles && filter.Distance != null)
            {
                filter.Distance = GeoManager.ConvertMilesToKM((double)filter.Distance);
            }

            bool filterByConnectionTypes = false;
            bool filterByLevels = false;
            bool filterByOperators = false;
            bool filterByCountries = false;
            bool filterByUsage = false;
            bool filterByStatus = false;
            bool filterByDataProvider = false;
            bool filterByChargePoints = false;

            if (filter.ConnectionTypeIDs != null) { filterByConnectionTypes = true; }
            else { filter.ConnectionTypeIDs = new int[] { -1 }; }

            if (filter.LevelIDs != null) { filterByLevels = true; }
            else { filter.LevelIDs = new int[] { -1 }; }

            if (filter.OperatorIDs != null) { filterByOperators = true; }
            else { filter.OperatorIDs = new int[] { -1 }; }

            if (filter.ChargePointIDs != null) { filterByChargePoints = true; }
            else { filter.ChargePointIDs = new int[] { -1 }; }

            //either filter by named country code or by country id list
            if (filter.CountryCode != null)
            {
                var referenceData = database.GetCollection<OCM.API.Common.Model.CoreReferenceData>("reference").FindOne();

                var filterCountry = referenceData.Countries.FirstOrDefault(c => c.ISOCode.ToUpper() == filter.CountryCode.ToUpper());
                if (filterCountry != null)
                {
                    filterByCountries = true;
                    filter.CountryIDs = new int[] { filterCountry.ID };
                }
                else
                {
                    filterByCountries = false;
                    filter.CountryIDs = new int[] { -1 };
                }
            }
            else
            {
                if (filter.CountryIDs != null && filter.CountryIDs.Any()) { filterByCountries = true; }
                else { filter.CountryIDs = new int[] { -1 }; }
            }

            if (filter.UsageTypeIDs != null) { filterByUsage = true; }
            else { filter.UsageTypeIDs = new int[] { -1 }; }

            if (filter.StatusTypeIDs != null) { filterByStatus = true; }
            else { filter.StatusTypeIDs = new int[] { -1 }; }

            if (filter.DataProviderIDs != null) { filterByDataProvider = true; }
            else { filter.DataProviderIDs = new int[] { -1 }; }

            if (filter.SubmissionStatusTypeID == -1) filter.SubmissionStatusTypeID = null;
            /////////////////////////////////////
            if (database != null)
            {

                System.Diagnostics.Debug.Print($"MongoDB cache building query @ {stopwatch.ElapsedMilliseconds}ms");

                var collection = database.GetCollection<OCM.API.Common.Model.ChargePoint>("poi");
                IQueryable<OCM.API.Common.Model.ChargePoint> poiList = from c in collection.AsQueryable<OCM.API.Common.Model.ChargePoint>() select c;

                System.Diagnostics.Debug.Print($"MongoDB got poiList as Queryable @ {stopwatch.ElapsedMilliseconds}ms");

                //filter by points along polyline or bounding box (TODO: polygon)
                if (
                    (filter.Polyline != null && filter.Polyline.Any())
                    || (filter.BoundingBox != null && filter.BoundingBox.Any())
                    || (filter.Polygon != null && filter.Polygon.Any())
                    )
                {
                    //override lat.long specified in search, use polyline or bounding box instead
                    filter.Latitude = null;
                    filter.Longitude = null;

                    double[,] pointList;

                    //filter by location within polyline expanded to a polygon

                    IEnumerable<LatLon> searchPolygon = null;

                    if (filter.Polyline != null && filter.Polyline.Any())
                    {
                        if (filter.Distance == null) filter.Distance = DefaultPolylineSearchDistanceKM;
                        searchPolygon = OCM.Core.Util.PolylineEncoder.SearchPolygonFromPolyLine(filter.Polyline, (double)filter.Distance);
                    }

                    if (filter.BoundingBox != null && filter.BoundingBox.Any())
                    {
                        // bounding box points could be in any order, so normalise here:
                        var polyPoints = Core.Util.PolylineEncoder.ConvertPointsToBoundingBox(filter.BoundingBox)
                                            .Coordinates
                                            .Select(p => new LatLon { Latitude = p.Y, Longitude = p.X }).ToList();

                        searchPolygon = polyPoints;
                    }

                    if (filter.Polygon != null && filter.Polygon.Any())
                    {
                        searchPolygon = filter.Polygon;
                    }

                    pointList = new double[searchPolygon.Count(), 2];
                    int pointIndex = 0;
                    foreach (var p in searchPolygon)
                    {
                        pointList[pointIndex, 0] = (double)p.Longitude;
                        pointList[pointIndex, 1] = (double)p.Latitude;
                        pointIndex++;
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(" {lat: " + p.Latitude + ", lng: " + p.Longitude + "},");
#endif
                    }
                    poiList = poiList.Where(q => Query.WithinPolygon("SpatialPosition.coordinates", pointList).Inject());
                }
                else
                {
                    if (requiresDistance)
                    {
                        //filter by distance from lat/lon first
                        if (filter.Distance == null) filter.Distance = DefaultLatLngSearchDistanceKM;
                        poiList = poiList.Where(q => Query.Near("SpatialPosition.coordinates", searchPoint, (double)filter.Distance * 1000).Inject());
                    }
                }

                int greaterThanId = 0;
                // workaround mongodb linq conversion bug
                if (filter.GreaterThanId.HasValue) greaterThanId = filter.GreaterThanId.Value;

                if (filter.Postcodes == null) filter.Postcodes = new string[] { };
                bool filterOnPostcodes = filter.Postcodes.Any();


                poiList = (from c in poiList
                           where

                                       (c.AddressInfo != null) &&
                                       ((filter.SubmissionStatusTypeID == null && (c.SubmissionStatusTypeID == null || c.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Imported_Published || c.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Submitted_Published))
                                             || (filter.SubmissionStatusTypeID == 0) //return all regardless of status
                                             || (filter.SubmissionStatusTypeID != null && c.SubmissionStatusTypeID != null && c.SubmissionStatusTypeID == filter.SubmissionStatusTypeID)
                                             ) //by default return live cps only, otherwise use specific submission statusid
                                       && (c.SubmissionStatusTypeID != null && c.SubmissionStatusTypeID != (int)StandardSubmissionStatusTypes.Delisted_NotPublicInformation)

                                       && (filter.OperatorName == null || c.OperatorInfo.Title == filter.OperatorName)
                                       && (filter.IsOpenData == null || (filter.IsOpenData != null && ((filter.IsOpenData == true && c.DataProvider.IsOpenDataLicensed == true) || (filter.IsOpenData == false && c.DataProvider.IsOpenDataLicensed != true))))
                                       && (!filter.GreaterThanId.HasValue || (filter.GreaterThanId.HasValue && c.ID > greaterThanId))
                                       && (filter.DataProviderName == null || c.DataProvider.Title == filter.DataProviderName)
                                       && (filterByCountries == false || (filterByCountries == true && filter.CountryIDs.Contains((int)c.AddressInfo.CountryID)))
                                       && (filterByOperators == false || (filterByOperators == true && filter.OperatorIDs.Contains((int)c.OperatorID)))
                                       && (filterByChargePoints == false || (filterByChargePoints == true && filter.ChargePointIDs.Contains(c.ID)))
                                       && (filterByUsage == false || (filterByUsage == true && filter.UsageTypeIDs.Contains((int)c.UsageTypeID)))
                                       && ((filterByStatus == false && c.StatusTypeID != (int)StandardStatusTypes.RemovedDecomissioned) || (filterByStatus == true && filter.StatusTypeIDs.Contains((int)c.StatusTypeID)))
                                       && (filterByDataProvider == false || (filterByDataProvider == true && filter.DataProviderIDs.Contains((int)c.DataProviderID)))
                                       && (filterOnPostcodes == false || (filterOnPostcodes == true && c.AddressInfo.Postcode != null && filter.Postcodes.Contains(c.AddressInfo.Postcode)))
                           select c);

                if (filter.ChangesFromDate != null)
                {
                    poiList = poiList.Where(c => c.DateLastStatusUpdate >= filter.ChangesFromDate.Value);
                }

                if (filter.CreatedFromDate != null)
                {
                    poiList = poiList.Where(c => c.DateCreated >= filter.CreatedFromDate.Value);
                }

                //where level of detail is greater than 1 we decide how much to return based on the given level of detail (1-10) Level 10 will return the least amount of data and is suitable for a global overview
                if (filter.LevelOfDetail > 1)
                {
                    //return progressively less matching results (across whole data set) as requested Level Of Detail gets higher

                    if (filter.LevelOfDetail > 3)
                    {
                        filter.LevelOfDetail = 1; //highest priority LOD
                    }
                    else
                    {
                        filter.LevelOfDetail = 2; //include next level priority items
                    }
                    poiList = poiList.Where(c => c.LevelOfDetail <= filter.LevelOfDetail);
                }

                //apply connectionInfo filters, all filters must match a distinct connection within the charge point, rather than any filter matching any connectioninfo
                if (filter.ConnectionType != null || filter.MinPowerKW != null || filterByConnectionTypes || filterByLevels)
                {
                    poiList = from c in poiList
                              where
                              c.Connections.Any(conn =>
                                    (filter.ConnectionType == null || (filter.ConnectionType != null && conn.ConnectionType.Title == filter.ConnectionType))
                                    && (filter.MinPowerKW == null || (filter.MinPowerKW != null && conn.PowerKW >= filter.MinPowerKW))
                                    && (filter.MaxPowerKW == null || (filter.MaxPowerKW != null && conn.PowerKW <= filter.MaxPowerKW))
                                    && (filterByConnectionTypes == false || (filterByConnectionTypes == true && filter.ConnectionTypeIDs.Contains(conn.ConnectionType.ID)))
                                    && (filterByLevels == false || (filterByLevels == true && filter.LevelIDs.Contains((int)conn.Level.ID)))
                                     )
                              select c;
                }

                System.Diagnostics.Debug.Print($"MongoDB executing query @ {stopwatch.ElapsedMilliseconds}ms");

                IEnumerable<API.Common.Model.ChargePoint> results = null;
                if (!requiresDistance || (filter.Latitude == null || filter.Longitude == null))
                {
                    //distance is not required or can't be provided
                    System.Diagnostics.Debug.Print($"MongoDB starting query to list @ {stopwatch.ElapsedMilliseconds}ms");

                    if (filter.SortBy == "created_asc")
                    {
                        results = poiList.OrderBy(p => p.DateCreated).Take(filter.MaxResults).AsEnumerable();
                    }
                    else if (filter.SortBy == "modified_asc")
                    {
                        results = poiList.OrderBy(p => p.DateLastStatusUpdate).Take(filter.MaxResults).AsEnumerable();
                    }
                    else if (filter.SortBy == "id_asc")
                    {
                        results = poiList.OrderBy(p => p.ID).Take(filter.MaxResults).AsEnumerable();
                    }
                    else
                    {
                        results = poiList.OrderByDescending(p => p.ID).Take(filter.MaxResults).AsEnumerable();
                    }

                    System.Diagnostics.Debug.Print($"MongoDB finished query to list @ {stopwatch.ElapsedMilliseconds}ms");
                }
                else
                {
                    //distance is required, calculate and populate in results
                    results = poiList.ToArray();
                    //populate distance
                    foreach (var p in results)
                    {
                        p.AddressInfo.Distance = GeoManager.CalcDistance((double)filter.Latitude, (double)filter.Longitude, p.AddressInfo.Latitude, p.AddressInfo.Longitude, filter.DistanceUnit);
                        p.AddressInfo.DistanceUnit = filter.DistanceUnit;
                    }
                    results = results.OrderBy(r => r.AddressInfo.Distance).Take(filter.MaxResults);
                }

                if (filter.IsCompactOutput)
                {
                    System.Diagnostics.Debug.Print($"MongoDB begin conversion to compact output @ {stopwatch.ElapsedMilliseconds}ms");

                    // we will be mutating the results so need to convert to object we can update
                    if (!(results is Array))
                    {
                        results = results.ToArray();
                    }

                    System.Diagnostics.Debug.Print($"MongoDB converted to array @ {stopwatch.ElapsedMilliseconds}ms");

                    // dehydrate POI object by removing navigation properties which are based on reference data. Client can then rehydrate using reference data, saving on data transfer KB
                    // TODO: find faster method or replace with custom serialization
                    foreach (var p in results)
                    {
                        //need to null reference data objects so they are not included in output. caution required here to ensure compact output via SQL is same as Cache DB output
                        p.DataProvider = null;
                        p.OperatorInfo = null;
                        p.UsageType = null;
                        p.StatusType = null;
                        p.SubmissionStatus = null;
                        p.AddressInfo.Country = null;
                        if (p.Connections != null)
                        {
                            foreach (var c in p.Connections)
                            {
                                c.ConnectionType = null;
                                c.CurrentType = null;
                                c.Level = null;
                                c.StatusType = null;
                            }
                        }

                        if (!filter.IncludeComments)
                        {
                            p.UserComments = null;
                            p.MediaItems = null;
                        }

                        if (p.UserComments != null)
                        {
                            foreach (var c in p.UserComments)
                            {
                                c.CheckinStatusType = null;
                                c.CommentType = null;
                            }
                        }
                    }
                }

                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine("Cache Provider POI Total Query Time:" + stopwatch.ElapsedMilliseconds + "ms");

                return results;
            }
            else
            {
                return null;
            }
        }

        public async Task<List<BenchmarkResult>> PerformPOIQueryBenchmark(int numQueries, string mode = "country")
        {
            List<BenchmarkResult> results = new List<BenchmarkResult>();

            for (int i = 0; i < numQueries; i++)
            {
                BenchmarkResult result = new BenchmarkResult();

                try
                {
                    result.Description = "Cached POI Query [" + mode + "] " + i;

                    APIRequestParams filter = new APIRequestParams();
                    filter.MaxResults = 100;

                    if (mode == "country")
                    {
                        filter.CountryCode = "NL";
                    }
                    else if (mode == "distance")
                    {
                        filter.Latitude = 57.10604;
                        filter.Longitude = -2.62214;
                        filter.Distance = 50;

                        filter.DistanceUnit = DistanceUnit.Miles;
                    }
                    else
                    {
                        var r = new Random(100).NextDouble();

                        filter.BoundingBox = new List<LatLon> { new LatLon { Latitude = -32.27537992647112 + (r / 100), Longitude = 114.88498474200799 + (r / 100) }, new LatLon { Latitude = -31.664884896457338 + (r / 100), Longitude = 116.45335732240358 + (r / 100) } };
                        filter.DistanceUnit = DistanceUnit.Miles;
                    }

                    var stopwatch = Stopwatch.StartNew();
                    var poiList = await this.GetPOIListAsync(filter);
                    stopwatch.Stop();
                    result.Description += " results:" + poiList.ToArray().Count();
                    result.TimeMS = stopwatch.ElapsedMilliseconds;
                }
                catch (Exception exp)
                {
                    result.Description += " Failed:" + exp.ToString();
                }

                results.Add(result);
            }
            return results;
        }
    }
}