using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.API.Common.Model.Extended;
using OCM.Core.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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
        [System.Text.Json.Serialization.JsonIgnore]
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
        private const int DefaultLatLngSearchDistanceKM = 50;

        private MongoClient client = null;
        private IMongoDatabase database = null;
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

                            _instance.EnsureMongoDBIndexes();
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
            database = client.GetDatabase(_settings.MongoDBSettings.DatabaseName);
            status = GetMirrorStatus(false, false).Result;

            if (status.StatusCode == HttpStatusCode.ExpectationFailed && _settings.IsCacheOnlyMode)
            {
                throw new Exception("MongoDB Cache Unavailable. Check configuration settings for connection string. ::" + status.Description);
            }
        }

        public IMongoCollection<POIMongoDB> GetPOICollection()
        {
            return database.GetCollection<POIMongoDB>("poi");
        }

        public async Task RemoveAllPOI(IEnumerable<OCM.API.Common.Model.ChargePoint> poiList, IMongoCollection<POIMongoDB> poiCollection)
        {
            foreach (var poi in poiList)
            {
                await poiCollection.DeleteManyAsync(p => p.ID == poi.ID);
            }
        }

        public async Task InsertAllPOI(IEnumerable<OCM.API.Common.Model.ChargePoint> poiList, IMongoCollection<POIMongoDB> poiCollection)
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
            await poiCollection.InsertManyAsync(mongoDBPoiList);
        }

        public async Task<List<OCM.API.Common.Model.ChargePoint>> GetPOIListToUpdate(OCMEntities dataModel, CacheUpdateStrategy updateStrategy, CoreReferenceData refData, int pageIndex = 0, int pageSize = 0)
        {
            // create if not exists
            if (database.GetCollection<BsonDocument>("poi") == null)
            {
                database.CreateCollection("poi");
            }

            IQueryable<Data.ChargePoint> dataList = dataModel.ChargePoints
                 .Include(a1 => a1.AddressInfo)
                      .ThenInclude(a => a.Country)
                 .Include(a1 => a1.ConnectionInfos)
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
                var maxPOI = await MongoDB.Driver.Linq.MongoQueryable.FirstOrDefaultAsync(
                    poiCollection.AsQueryable().OrderByDescending(s => s.DateLastStatusUpdate)
                );

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
                var maxPOI = await MongoDB.Driver.Linq.MongoQueryable.FirstOrDefaultAsync(
                    poiCollection.AsQueryable().OrderByDescending(s => s.DateLastStatusUpdate)
                );

                DateTime? dateLastModified = null;
                if (maxPOI != null) { dateLastModified = maxPOI.DateLastStatusUpdate.Value.AddMinutes(-10); }

                //determine POI updated since last status update we have in cache
                var stopwatch = Stopwatch.StartNew();

                dataList = dataList.Where(o => o.DateLastStatusUpdate > dateLastModified);

                stopwatch.Stop();

                System.Diagnostics.Debug.WriteLine($"POI List retrieved in {stopwatch.Elapsed.TotalSeconds} seconds");

                var poiList = new List<OCM.API.Common.Model.ChargePoint>();
                stopwatch.Restart();

                foreach (var cp in dataList)
                {
                    poiList.Add(OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(cp, true, true, true, true, refData));
                }

                System.Diagnostics.Debug.WriteLine($"POI List model prepared in {stopwatch.Elapsed.TotalSeconds} seconds");

                return poiList;
            }

            if (updateStrategy == CacheUpdateStrategy.All)
            {
                var stopwatch = Stopwatch.StartNew();

                // get data list, include navigation properties to improve query performance
                var list = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(dataList);

                stopwatch.Stop();

                System.Diagnostics.Debug.WriteLine($"POI List retrieved in {stopwatch.Elapsed.TotalSeconds} seconds");
                stopwatch.Restart();

                var poiList = new List<OCM.API.Common.Model.ChargePoint>();

                foreach (var cp in list)
                {
                    poiList.Add(OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(cp, true, true, true, true, refData));

                    if (poiList.Count % 100 == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"POIs processed {poiList.Count} in {stopwatch.Elapsed.TotalSeconds} seconds");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"POI List model prepared in {stopwatch.Elapsed.TotalSeconds} seconds");

                return poiList;
            }

            return null;

        }

        public async Task<MirrorStatus> RefreshCachedPOI(int poiId)
        {

            var refData = await new ReferenceDataManager().GetCoreReferenceDataAsync();
            var dataModel = new OCMEntities();
            var poiModel = dataModel.ChargePoints.FirstOrDefault(p => p.Id == poiId);

            var poiCollection = database.GetCollection<POIMongoDB>("poi");

            if (poiModel != null)
            {
                var cachePOI = POIMongoDB.FromChargePoint(OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(poiModel, refData));
                if (cachePOI.AddressInfo != null)
                {
                    cachePOI.SpatialPosition = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(cachePOI.AddressInfo.Longitude, cachePOI.AddressInfo.Latitude));
                }

                // replace/insert POI
                await poiCollection.ReplaceOneAsync(q => q.ID == poiId, cachePOI, new ReplaceOptions { IsUpsert = true });
            }
            else
            {
                //poi not present in master DB, we've removed it from cache
                var removeResult = poiCollection.DeleteMany(q => q.ID == poiId);
                System.Diagnostics.Debug.WriteLine("POIs removed from cache [" + poiId + "]:" + removeResult.DeletedCount);
            }

            long numPOIInMasterDB = dataModel.ChargePoints.LongCount();
            return await RefreshMirrorStatus(await poiCollection.EstimatedDocumentCountAsync(), 1, numPOIInMasterDB);

        }
        public async Task RemoveMediaItemFromPOI(int poiId, int mediaItemId)
        {
            if (poiId <= 0 || mediaItemId <= 0) return;

            var poiCollection = database.GetCollection<BsonDocument>("poi");
            var filter = new BsonDocument("ID", poiId);

            var update = new BsonDocument(
                "$pull",
                new BsonDocument("MediaItems", new BsonDocument("ID", mediaItemId))
            );

            await poiCollection.UpdateOneAsync(filter, update);
        }
        /// <summary>
        /// Ensure all MongoDB indexes are being set up.
        /// </summary>
        /// <returns></returns>
        protected void EnsureMongoDBIndexes()
        {
            var poiCollection = database.GetCollection<POIMongoDB>("poi");
            poiCollection.Indexes.CreateOne(Builders<POIMongoDB>.IndexKeys.Geo2DSphere("SpatialPosition")); // bounding box queries (geoWithin > geometry > polygon)
            //poiCollection.CreateIndex(IndexKeys.GeoSpatial("SpatialPosition.coordinates")); // bounding box queries
            //poiCollection.CreateIndex(IndexKeys<POIMongoDB>.GeoSpatialSpherical(x => x.SpatialPosition)); // distance queries
            poiCollection.Indexes.CreateOne(Builders<POIMongoDB>.IndexKeys.Descending(x => x.DateLastStatusUpdate));
            poiCollection.Indexes.CreateOne(Builders<POIMongoDB>.IndexKeys.Descending(x => x.DateCreated));
            poiCollection.Indexes.CreateOne(Builders<POIMongoDB>.IndexKeys.Descending(x => x.ID));
        }

        /// <summary>
        /// Perform full or partial repopulation of POI Mirror in MongoDB
        /// </summary>
        /// <returns></returns>
        public async Task<MirrorStatus> PopulatePOIMirror(CacheUpdateStrategy updateStrategy, ILogger logger = null)
        {
            bool preserveExistingPOIs = true;

            EnsureMongoDBIndexes();

            // cache will refresh either from the source database or via a master API
            if (!_settings.IsCacheOnlyMode)
            {
                using (var dataModel = new Data.OCMEntities())
                {

                    if (database.GetCollection<BsonDocument>("poi") == null)
                    {
                        database.CreateCollection("poi");
                    }

                    if (updateStrategy != CacheUpdateStrategy.All)
                    {
                        if (database.GetCollection<BsonDocument>("reference") == null)
                        {
                            database.CreateCollection("reference");
                        }
                        if (database.GetCollection<BsonDocument>("status") == null)
                        {
                            database.CreateCollection("status");
                        }
                        if (database.GetCollection<BsonDocument>("poi") == null)
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
                        coreRefData = await refDataManager.GetCoreReferenceDataAsync(new APIRequestParams { AllowDataStoreDB = true, AllowMirrorDB = false });
                    }

                    if (coreRefData != null)
                    {
                        var reference = database.GetCollection<CoreReferenceData>("reference");
                        await reference.ReplaceOneAsync(f => f.ConnectionTypes != null, coreRefData, new ReplaceOptions { IsUpsert = true });
                    }

                    var batchSize = 1000;

                    List<API.Common.Model.ChargePoint> poiList;

                    var poiCollection = database.GetCollection<POIMongoDB>("poi");
                    if (updateStrategy != CacheUpdateStrategy.All)
                    {
                        poiList = await GetPOIListToUpdate(dataModel, updateStrategy, coreRefData);


                        if (poiList != null && poiList.Any())
                        {

                            await RemoveAllPOI(poiList, poiCollection);

                            await Task.Delay(300);
                            await InsertAllPOI(poiList, poiCollection);
                        }
                    }
                    else
                    {
                        // full refresh

                        await poiCollection.DeleteManyAsync(p => p.ID > 0);

                        await Task.Delay(300);

                        var pageIndex = 0;
                        var pageSize = 5000;
                        var total = 0;

                        //get batch of poi to insert
                        var sw = Stopwatch.StartNew();
                        poiList = await GetPOIListToUpdate(dataModel, updateStrategy, coreRefData, pageIndex, pageSize);
                        while (poiList.Count > 0)
                        {
                            logger?.LogInformation($"Inserting batch {pageIndex}");

                            System.Diagnostics.Debug.WriteLine($"Inserting batch {pageIndex} :: {pageIndex * pageSize}");

                            try
                            {
                                await InsertAllPOI(poiList, poiCollection);
                            }
                            catch (Exception ex)
                            {
                                logger?.LogError($"Failed to insert batch {pageIndex} POI {poiList[0].ID} onwards. {ex.Message}");
                            }

                            pageIndex++;
                            total += poiList.Count;

                            poiList = await GetPOIListToUpdate(dataModel, updateStrategy, coreRefData, pageIndex, pageSize);
                        }

                        //poiCollection.Indexes.ReIndex();

                        sw.Stop();
                        GC.Collect();
                        System.Diagnostics.Debug.WriteLine($"Rebuild of complete cache took {sw.Elapsed.TotalSeconds}s");

                    }

                    long numPOIInMasterDB = dataModel.ChargePoints.LongCount();
                    return await RefreshMirrorStatus(await poiCollection.EstimatedDocumentCountAsync(), 1, numPOIInMasterDB);
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
                    syncStatus.DataHash ??= "";

                    if (syncStatus != null)
                    {
                        var poiCollection = database.GetCollection<POIMongoDB>("poi");

                        bool isRefDataSyncRequired = false;
                        DateTime? lastUpdated = null;
                        DateTime? lastCreated = null;
                        int maxIdCached = 0;

                        if (await poiCollection.EstimatedDocumentCountAsync() == 0)
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
                            localHash ??= "";

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
                                    var reference = database.GetCollection<CoreReferenceData>("reference");
                                    await reference.ReplaceOneAsync(f => f.ConnectionTypes != null, coreRefData, new ReplaceOptions { IsUpsert = true });
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
                                            await poiCollection.DeleteManyAsync(p => p.ID > 0);
                                        }
                                        else
                                        {
                                            await RemoveAllPOI(poiList, poiCollection);
                                        }
                                    }

                                    Thread.Sleep(300);
                                    await InsertAllPOI(poiList, poiCollection);

                                    if (updateStrategy == CacheUpdateStrategy.All)
                                    {
                                        // poiCollection.ReIndex();
                                    }

                                    numPOIUpdated = poiList.LongCount();
                                }

                            }

                            var poiCount = await poiCollection.EstimatedDocumentCountAsync();
                            var status = await RefreshMirrorStatus(poiCount, numPOIUpdated, poiCount, dateLastSync);
                            status.MaxBatchSize = _settings.MongoDBSettings.CacheSyncBatchSize;
                            return status;

                        }
                    }
                }
            }

            // nothing to update
            return await GetMirrorStatus(false, false, false);
        }

        private async Task<MirrorStatus> RefreshMirrorStatus(
            long cachePOICollectionCount, long numPOIUpdated, long numPOIInMasterDB,
            DateTime? poiLastUpdate = null,
            DateTime? poiLastCreated = null
            )
        {
            var statusCollection = database.GetCollection<MirrorStatus>("status");

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

            await statusCollection.ReplaceOneAsync(s => s.LastUpdated > DateTime.MinValue, status, new ReplaceOptions { IsUpsert = true });
            return status;
        }

        private string GetCacheContentHash()
        {
            var cols = new string[] { "poi", "reference", "countryinfo" };

            /* var hashCommand = new Command<BsonDocumentCommand> {
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
             }*/

            return null;
        }

        public async Task<MirrorStatus> GetMirrorStatus(bool includeDupeCheck, bool includeDBCheck = true, bool includeContentHash = false)
        {
            try
            {
                var statusCollection = database.GetCollection<MirrorStatus>("status");
                var currentStatus = await MongoDB.Driver.Linq.MongoQueryable.FirstOrDefaultAsync(
                    statusCollection.AsQueryable()
                );

                if (currentStatus == null)
                {
                    return new MirrorStatus { StatusCode = HttpStatusCode.NotFound, Description = "Cache is offline (not yet generated)" };
                }

                if (includeContentHash)
                {
                    currentStatus.ContentHash = GetCacheContentHash();
                }

                if (includeDBCheck)
                {
                    using (var db = new OCMEntities())
                    {
                        currentStatus.TotalPOIInDB = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.LongCountAsync(db.ChargePoints);
                        currentStatus.LastPOIUpdate = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.MaxAsync(db.ChargePoints, i => i.DateLastStatusUpdate);
                        currentStatus.LastPOICreated = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.MaxAsync(db.ChargePoints, i => i.DateCreated);
                        currentStatus.MaxPOIId = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.MaxAsync(db.ChargePoints, i => i.Id);
                    }
                }
                else
                {
                    var poiCollection = database.GetCollection<POIMongoDB>("poi");
                    currentStatus.TotalPOIInDB = await poiCollection.EstimatedDocumentCountAsync();
                    currentStatus.LastPOIUpdate = poiCollection.AsQueryable().Max(p => p.DateLastStatusUpdate);
                    currentStatus.LastPOICreated = poiCollection.AsQueryable().Max(p => p.DateCreated);
                    currentStatus.MaxPOIId = poiCollection.AsQueryable().Max(p => p.ID);
                }

                if (includeDupeCheck)
                {
                    //perform check to see if number of distinct ids!= number of pois
                    var poiCollection = database.GetCollection<POIMongoDB>("poi");
                    currentStatus.NumDistinctPOIs = await MongoDB.Driver.Linq.MongoQueryable.CountAsync(
                        poiCollection.AsQueryable().DistinctBy(d => d.ID)
                    );
                }

                var serverInfo = client.Settings.Servers.FirstOrDefault();
                currentStatus.Server = $"{serverInfo.Host}:{serverInfo.Port}";
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
                return results.AsQueryable().ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> IsCacheReady()
        {
            if (status != null && (_settings.MongoDBSettings.MaxCacheAgeMinutes == 0 || status.LastUpdated.AddMinutes(_settings.MongoDBSettings.MaxCacheAgeMinutes) > DateTime.UtcNow))
            {
                return true;
            }
            else
            {
                status = await GetMirrorStatus(false, false);
                return status.NumDistinctPOIs > 0;
            }
        }
        public async Task<CoreReferenceData> GetCoreReferenceData(APIRequestParams filter)
        {
            try
            {
                if (await IsCacheReady())
                {
                    var refDataCollection = database.GetCollection<CoreReferenceData>("reference");
                    var refData = refDataCollection.AsQueryable().FirstOrDefault();

                    if (filter.CountryIDs != null && filter.CountryIDs.Any())
                    {
                        // need to filter results based on usage by country
                        var poiCollection = database.GetCollection<POIMongoDB>("poi").AsQueryable();

                        var connectionsInCountry = poiCollection.Where(poi =>
                                                        poi.AddressInfo.CountryID != null
                                                        && filter.CountryIDs.Contains((int)poi.AddressInfo.CountryID)
                                                        && (poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Imported_Published || poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Submitted_Published)
                                                        && poi.Connections.Any()
                                                   )
                                                    .Select(p => new
                                                    {
                                                        CountryID = p.AddressInfo.CountryID,
                                                        ConnectionTypes = p.Connections.Select(t => t.ConnectionTypeID)
                                                    })
                                                    .SelectMany(p => p.ConnectionTypes, (i, c) => new { CountryId = i.CountryID, ConnectionTypeId = c })
                                                    .ToArray()
                                                    .Distinct();

                        refData.ConnectionTypes.RemoveAll(a => !connectionsInCountry.Any(r => r.ConnectionTypeId == a.ID));


                        if (filter.FilterOperatorsOnCountry)
                        {
                            // filter on operators present within given countries

                            var operatorsInCountry = poiCollection.Where(poi =>
                                                        poi.AddressInfo.CountryID != null
                                                        && filter.CountryIDs.Contains((int)poi.AddressInfo.CountryID)
                                                        && (poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Imported_Published || poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Submitted_Published)
                                                        && poi.OperatorID != null)
                                                        .Select(p => new { CountryId = p.AddressInfo.CountryID, OperatorId = p.OperatorID })
                                                        .ToArray()
                                                        .Distinct();

                            refData.Operators.RemoveAll(a => !operatorsInCountry.Any(r => r.OperatorId == a.ID));
                        }

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
        public async Task<OCM.API.Common.Model.ChargePoint> GetPOI(int id)
        {
            if (await IsCacheReady())
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

            if (!await IsCacheReady())
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

            //either filter by named country code or by country id list
            if (filter.CountryCode != null)
            {
                var referenceData = await GetCoreReferenceData(new APIRequestParams { });

                var filterCountry = referenceData.Countries.FirstOrDefault(c => c.ISOCode.ToUpper() == filter.CountryCode.ToUpper());
                if (filterCountry != null)
                {
                    filter.CountryIDs = new int[] { filterCountry.ID };
                }

            }

            /////////////////////////////////////
            if (database != null)
            {

                System.Diagnostics.Debug.Print($"MongoDB cache building query @ {stopwatch.ElapsedMilliseconds}ms");

                var collection = database.GetCollection<POIMongoDB>("poi");

                IQueryable<POIMongoDB> poiList = collection.AsQueryable();

                System.Diagnostics.Debug.Print($"MongoDB got poiList as Queryable @ {stopwatch.ElapsedMilliseconds}ms");

                //filter by points along polyline, bounding box or polygon


                if (
                    (filter.Polyline != null && filter.Polyline.Any())
                    || (filter.BoundingBox != null && filter.BoundingBox.Any())
                    || (filter.Polygon != null && filter.Polygon.Any())
                    )
                {
                    //override lat.long specified in search, use polyline or bounding box instead
                    filter.Latitude = null;
                    filter.Longitude = null;

                    //filter by location within polyline expanded to a polygon

                    IEnumerable<LatLon> searchPolygon = null;

                    if (filter.Polyline != null && filter.Polyline.Any())
                    {
                        if (filter.Distance == null) filter.Distance = DefaultPolylineSearchDistanceKM;
                        searchPolygon = OCM.Core.Util.PolylineEncoder.SearchPolygonFromPolyLine(filter.Polyline, (double)filter.Distance);
                    }
                    else if (filter.BoundingBox != null && filter.BoundingBox.Any())
                    {
                        // bounding box points could be in any order, so normalise here:
                        var polyPoints = Core.Util.PolylineEncoder.ConvertPointsToBoundingBox(filter.BoundingBox)
                                            .Coordinates
                                            .Select(p => new LatLon { Latitude = p.Y, Longitude = p.X }).ToList();

                        searchPolygon = polyPoints;
                    }
                    else if (filter.Polygon != null && filter.Polygon.Any())
                    {
                        //searchPolygon = filter.Polygon;
                        searchPolygon = OCM.Core.Util.PolylineEncoder.SearchPolygonFromPoints(filter.Polygon);
                    }

                    var geoCoords = searchPolygon.Select(t => new GeoJson2DGeographicCoordinates((double)t.Longitude, (double)t.Latitude));
                    var linearRing = new GeoJsonLinearRingCoordinates<GeoJson2DGeographicCoordinates>(geoCoords.ToArray());

                    var geometry = new GeoJsonPolygon<GeoJson2DGeographicCoordinates>(new GeoJsonPolygonCoordinates<GeoJson2DGeographicCoordinates>(linearRing));

                    var polygonQueryBson = geometry.ToBsonDocument();
                    var crsDoc = BsonDocument.Parse("{ type: \"name\",  properties: { name: \"urn:x-mongodb:crs:strictwinding:EPSG:" + GeoManager.StandardSRID + "\" } }");
                    polygonQueryBson.Add("crs", crsDoc);

                    var geoJson = polygonQueryBson.ToJson();

                    // var geoFilter = Builders<POIMongoDB>.Filter.GeoWithin(x => x.SpatialPosition, polygonQueryBson);
                    var geoQuery = "{\"SpatialPosition\": {\"$geoWithin\": {\"$geometry\": " + geoJson + " } } }";
                    var geoBson = BsonDocument.Parse(geoQuery);
                    poiList = (await collection.FindAsync(geoBson)).ToEnumerable().AsQueryable();

                }
                else if (requiresDistance)
                {
                    //filter by distance from lat/lon first
                    if (filter.Distance == null) filter.Distance = DefaultLatLngSearchDistanceKM;

                    var geoFilter = Builders<POIMongoDB>.Filter.NearSphere(p => p.SpatialPosition, searchPoint, (double)filter.Distance * 1000);

                    poiList = (await collection.FindAsync(geoFilter)).ToEnumerable().AsQueryable();
                }

                poiList = ApplyQueryFilters(filter, poiList);

                System.Diagnostics.Debug.Print($"MongoDB executing query @ {stopwatch.ElapsedMilliseconds}ms");

                IQueryable<API.Common.Model.ChargePoint> results = null;
                if (!requiresDistance || (filter.Latitude == null || filter.Longitude == null))
                {
                    //distance is not required or can't be provided
                    System.Diagnostics.Debug.Print($"MongoDB starting query to list @ {stopwatch.ElapsedMilliseconds}ms");

                    if (filter.SortBy == "created_asc")
                    {
                        results = poiList.OrderBy(p => p.DateCreated).Take(filter.MaxResults).AsQueryable();
                    }
                    else if (filter.SortBy == "modified_asc")
                    {
                        results = poiList.OrderBy(p => p.DateLastStatusUpdate).Take(filter.MaxResults).AsQueryable();
                    }
                    else if (filter.SortBy == "id_asc")
                    {
                        results = poiList.OrderBy(p => p.ID).Take(filter.MaxResults).AsQueryable();
                    }
                    else
                    {
                        if (filter.BoundingBox == null || !filter.BoundingBox.Any())
                        {
                            poiList = poiList.OrderByDescending(p => p.ID);
                        }
                        // In boundingbox more, if no sorting was requested by the user,
                        // do not perform any sorting for performance reasons.
                        results = poiList.Take(filter.MaxResults);
                    }

                    System.Diagnostics.Debug.Print($"MongoDB finished query to list @ {stopwatch.ElapsedMilliseconds}ms");
                }
                else
                {
                    //distance is required, calculate and populate in results, mutate result set with distance unit
                    results = poiList.ToArray().AsQueryable();
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
                        results = results.ToArray().AsQueryable();
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

                var output = results.AsEnumerable();
                return output;
            }
            else
            {
                return null;
            }
        }

        public static IQueryable<POIMongoDB> ApplyQueryFilters(APIRequestParams filter, IQueryable<POIMongoDB> poiList)
        {
            int greaterThanId = 0;
            // workaround mongodb linq conversion bug
            if (filter.GreaterThanId.HasValue) greaterThanId = filter.GreaterThanId.Value;

            if (filter.OperatorIDs?.Any() == true)
            {
                poiList = poiList.Where(c => c.OperatorID != null && filter.OperatorIDs.Contains((int)c.OperatorID));
            }


            if (filter.SubmissionStatusTypeID?.Any(t => t > 0) == true)
            {
                //specific submission status
                poiList = poiList.Where(c => c.SubmissionStatusTypeID != null && filter.SubmissionStatusTypeID.Contains((int)c.SubmissionStatusTypeID));
            }
            else
            {
                // default to published submissions
                poiList = poiList.Where(c => c.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Imported_Published || c.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Submitted_Published);
            }

            // exclude any delisted POIs
            poiList = poiList.Where(c => c.SubmissionStatusTypeID != (int)StandardSubmissionStatusTypes.Delisted_NotPublicInformation);


            // deprecated filter by operator name
            if (filter.OperatorName != null)
            {
                poiList = poiList.Where(c => c.OperatorInfo.Title == filter.OperatorName);
            }


            if (filter.IsOpenData != null)
            {
                poiList = poiList.Where(c => (filter.IsOpenData == true && c.DataProvider.IsOpenDataLicensed == true) || (filter.IsOpenData == false && c.DataProvider.IsOpenDataLicensed != true));
            }


            if (filter.GreaterThanId.HasValue == true)
            {
                poiList = poiList.Where(c => filter.GreaterThanId.HasValue && c.ID > greaterThanId);
            }

            // deprecated filter by dataprovider name
            if (filter.DataProviderName != null)
            {
                poiList = poiList.Where(c => c.DataProvider.Title == filter.DataProviderName);
            }

            if (filter.CountryIDs?.Any() == true)
            {
                poiList = poiList.Where(c => c.AddressInfo.CountryID != null && filter.CountryIDs.Contains((int)c.AddressInfo.CountryID));
            }


            if (filter.ChargePointIDs?.Any() == true)
            {
                poiList = poiList.Where(c => filter.ChargePointIDs.Contains((int)c.ID));
            }

            if (filter.UsageTypeIDs?.Any() == true)
            {
                poiList = poiList.Where(c => c.UsageTypeID != null && filter.UsageTypeIDs.Contains((int)c.UsageTypeID));
            }


            if (filter.StatusTypeIDs?.Any() == true)
            {
                poiList = poiList.Where(c => c.StatusTypeID != null && filter.StatusTypeIDs.Contains((int)c.StatusTypeID));
            }

            // exclude any decomissioned items
            poiList = poiList.Where(c => c.StatusTypeID != (int)StandardStatusTypes.RemovedDecomissioned && c.StatusTypeID != (int)StandardStatusTypes.RemovedDuplicate);

            if (filter.DataProviderIDs?.Any() == true)
            {
                poiList = poiList.Where(c => c.DataProviderID != null && filter.DataProviderIDs.Contains((int)c.DataProviderID));
            }

            if (filter.Postcodes?.Any() == true)
            {
                poiList = poiList.Where(c => filter.Postcodes.Contains(c.AddressInfo.Postcode));
            }


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
            if (filter.ConnectionType != null)
            {
                poiList = poiList.Where(c => c.Connections.Any(conn => conn.ConnectionType.Title == filter.ConnectionType));
            }

            if (filter.MinPowerKW != null)
            {
                poiList = poiList.Where(c => c.Connections.Any(conn => conn.PowerKW >= filter.MinPowerKW));
            }

            if (filter.MaxPowerKW != null)
            {
                poiList = poiList.Where(c => c.Connections.Any(conn => conn.PowerKW <= filter.MaxPowerKW));
            }

            if (filter.ConnectionTypeIDs?.Any() == true)
            {
                poiList = poiList.Where(c => c.Connections.Any(conn => conn.ConnectionTypeID != null && filter.ConnectionTypeIDs.Contains((int)conn.ConnectionTypeID)));
            }

            if (filter.LevelIDs?.Any() == true)
            {
                poiList = poiList.Where(c => c.Connections.Any(conn => conn.LevelID != null && filter.LevelIDs.Contains((int)conn.LevelID)));
            }

            poiList = poiList.Where(c => c.AddressInfo != null);
            return poiList;
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

    public static class EfExtensions
    {
        // https://expertcodeblog.wordpress.com/2018/02/19/net-core-2-0-resolve-error-the-source-iqueryable-doesnt-implement-iasyncenumerable/

        public static Task<List<TSource>> ToListAsyncSafe<TSource>(
          this IQueryable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (!(source is IAsyncEnumerable<TSource>))
                return Task.FromResult(source.ToList());
            return Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(source);
        }
    }
}