
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Configuration;
using OCM.API.Common;
using OCM.API.Common.Model;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GeoJsonObjectModel;
using Newtonsoft.Json;

namespace OCM.Core.Data
{
    public class POIMongoDB : OCM.API.Common.Model.ChargePoint
    {
        [JsonIgnore]
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> SpatialPosition { get; set; }

        public static POIMongoDB FromChargePoint(OCM.API.Common.Model.ChargePoint cp)
        {
            POIMongoDB poi = new POIMongoDB();
            poi.AddressInfo = cp.AddressInfo;
            poi.Chargers = cp.Chargers;
            poi.Connections = cp.Connections;
            poi.Contributor = cp.Contributor;
            poi.DataProvider = cp.DataProvider;
            poi.DataProviderID = cp.DataProviderID;
            poi.DataProvidersReference = cp.DataProvidersReference;
            poi.DataQualityLevel = cp.DataQualityLevel;
            poi.DateCreated = cp.DateCreated;
            poi.DateLastConfirmed = cp.DateLastConfirmed;
            poi.DateLastStatusUpdate = cp.DateLastStatusUpdate;
            poi.DatePlanned = cp.DatePlanned;
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
            poi.UUID = cp.UUID;
            return poi;
        }
    }

    public class APIProviderMongoDB
    {
        MongoDatabase database = null;
        MongoClient client = null;
        MongoServer server = null;

        public APIProviderMongoDB()
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
            }
            catch (Exception)
            {
                ; ;
            }

            var connectionString = ConfigurationManager.AppSettings["MongoDB_ConnectionString"].ToString();
            client = new MongoClient(connectionString);
            server = client.GetServer();
            database = server.GetDatabase(ConfigurationManager.AppSettings["MongoDB_Database"]);
        }

        public void PopulatePOIMirror(List<OCM.API.Common.Model.ChargePoint> poiList, CoreReferenceData coreRefData){
            if (!database.CollectionExists("poi")){
                database.CreateCollection("poi");
            }
            if (!database.CollectionExists("reference")){
                database.CreateCollection("reference");
            }

            if (coreRefData != null)
            {
                var reference = database.GetCollection<CoreReferenceData>("reference");
                reference.RemoveAll();
                reference.Insert(coreRefData);
            }

            if (poiList != null && poiList.Any())
            {
                
                var poi = database.GetCollection<POIMongoDB>("poi");
                poi.RemoveAll();

                List<POIMongoDB> insertList = new List<POIMongoDB>();
                foreach (var currentPOI in poiList)
                {
                    var newPoi = POIMongoDB.FromChargePoint(currentPOI);
                    newPoi.SpatialPosition = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(newPoi.AddressInfo.Longitude, newPoi.AddressInfo.Latitude));
                    insertList.Add(newPoi);
                }
                poi.InsertBatch(poiList);

                poi.EnsureIndex(IndexKeys<POIMongoDB>.GeoSpatialSpherical(x => x.SpatialPosition));
               
       
                //create spatial index
               // poiCollection.ensureIndex({ SpatialPosition: "2dsphere" }, { "name": "SpatialIndex" })
                
            }
        }
            
        public List<OCM.API.Common.Model.ChargePoint> GetPOIList(APIRequestSettings settings)
        {
            //TODO: share common between POIManager and this
            int maxResults = settings.MaxResults;

            bool requiresDistance = false;
            GeoJsonPoint<GeoJson2DGeographicCoordinates> searchPoint = null;

            if (settings.Latitude != null && settings.Longitude != null)
            {
                requiresDistance = true;
                maxResults = 10000; //TODO find way to prefilter on distance.
                if (settings.Distance == null) settings.Distance = 100;
                searchPoint = GeoJson.Point(GeoJson.Geographic((double)settings.Longitude, (double)settings.Latitude));
            }
            else
            {
                searchPoint = GeoJson.Point(GeoJson.Geographic(0,0));
                settings.Distance = 100;
               
            }



            //if distance filter provided in miles, convert to KM before use
            

            if (settings.DistanceUnit == OCM.API.Common.Model.DistanceUnit.Miles && settings.Distance != null)
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

            //TODO: get country id for given country code
            //either filter by named country code or by country id list
            if (settings.CountryCode != null)
            {
                var referenceData = database.GetCollection<OCM.API.Common.Model.CoreReferenceData>("reference").FindOne();

                var filterCountry = referenceData.Countries.FirstOrDefault(c => c.ISOCode == settings.CountryCode);
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
            /////////////////////////////////////
            if (database != null)
            {
                var collection = database.GetCollection<OCM.API.Common.Model.ChargePoint>("poi");
                IQueryable<OCM.API.Common.Model.ChargePoint> poiList = from c in collection.AsQueryable<OCM.API.Common.Model.ChargePoint>() select c;
                
                if (requiresDistance)
                {
                    //filter by distance first
                    poiList = poiList.Where(q => Query.Near("SpatialPosition", searchPoint, (double)settings.Distance * 1000).Inject());
                }

                poiList = from c in poiList
                              where

                                         // (c.AddressInfo != null && c.AddressInfo.Latitude != null && c.AddressInfo.Longitude != null && c.AddressInfo.CountryID != null)
                                          ((settings.SubmissionStatusTypeID == null && (c.SubmissionStatusTypeID == null || c.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Imported_Published || c.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Submitted_Published))
                                                || (settings.SubmissionStatusTypeID == 0) //return all regardless of status
                                                || (settings.SubmissionStatusTypeID != null && c.SubmissionStatusTypeID != null && c.SubmissionStatusTypeID == settings.SubmissionStatusTypeID)
                                                ) //by default return live cps only, otherwise use specific submission statusid
                                          && (c.SubmissionStatusTypeID != null && c.SubmissionStatusTypeID != (int)StandardSubmissionStatusTypes.Delisted_NotPublicInformation)
                                  // && (settings.ChargePointID == null || (settings.ChargePointID!=null && (c.ID!=null && c.ID == settings.ChargePointID)))
                                          && (settings.OperatorName == null || c.OperatorInfo.Title == settings.OperatorName)
                                          && (settings.IsOpenData == null || (settings.IsOpenData != null && ((settings.IsOpenData == true && c.DataProvider.IsOpenDataLicensed == true) || (settings.IsOpenData == false && c.DataProvider.IsOpenDataLicensed != true))))
                                          && (settings.DataProviderName == null || c.DataProvider.Title == settings.DataProviderName)
                                  // && (settings.LocationTitle == null || ((settings.LocationTitle!=null && c.AddressInfo!=null && c.AddressInfo.Title.Contains(settings.LocationTitle))))
                                          && (settings.ConnectionType == null || c.Connections.Any(conn => conn.ConnectionType.Title == settings.ConnectionType))
                                          && (settings.MinPowerKW == null || c.Connections.Any(conn => conn.PowerKW >= settings.MinPowerKW))

                                          && (filterByCountries == false || (filterByCountries == true && settings.CountryIDs.Contains((int)c.AddressInfo.CountryID)))
                                          && (filterByConnectionTypes == false || (filterByConnectionTypes == true && c.Connections.Any(conn => settings.ConnectionTypeIDs.Contains(conn.ConnectionType.ID))))
                                          && (filterByLevels == false || (filterByLevels == true && c.Connections.Any(chg => settings.LevelIDs.Contains((int)chg.Level.ID))))
                                          && (filterByOperators == false || (filterByOperators == true && settings.OperatorIDs.Contains((int)c.OperatorID)))
                                          && (filterByUsage == false || (filterByUsage == true && settings.UsageTypeIDs.Contains((int)c.UsageTypeID)))
                                          && (filterByStatus == false || (filterByStatus == true && settings.StatusTypeIDs.Contains((int)c.StatusTypeID)))
                                          && (filterByDataProvider == false || (filterByDataProvider == true && settings.DataProviderIDs.Contains((int)c.DataProviderID)))
                              select c;

                
                return poiList.ToList();
            }
            else
            {
                return null;
            }
        }
    }
}
