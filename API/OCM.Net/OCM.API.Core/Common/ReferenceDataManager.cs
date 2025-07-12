using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using OCM.API.Common.Model;

namespace OCM.API.Common
{
    public class ReferenceDataManager : ManagerBase
    {
        /// <summary>
        /// When filtering, min instance count of a connection type before it is included in results
        /// </summary>
        private const int MINFILTER_CONNECTIONTYPE_INSTANCES = 3;

        private const int MINFILTER_OPERATOR_INSTANCES = 1;

        public Country GetCountryByName(string country)
        {
            if (country == null) return null;
            country = country.ToLower();
            var selectedCountry = dataModel.Countries.FirstOrDefault(c => c.Title.ToLower() == country.ToLower() || c.Title.ToLower().Replace(" ", "") == country.Replace(" ", ""));
            return OCM.API.Common.Model.Extensions.Country.FromDataModel(selectedCountry);
        }

        public Country GetCountryByISO(string countryISO)
        {
            if (countryISO == null) return null;

            countryISO = countryISO.Trim().ToUpper();

            var selectedCountry = dataModel.Countries.FirstOrDefault(c => c.Isocode == countryISO);
            return OCM.API.Common.Model.Extensions.Country.FromDataModel(selectedCountry);
        }

        public Country GetCountry(int countryId)
        {
            return OCM.API.Common.Model.Extensions.Country.FromDataModel(dataModel.Countries.Find(countryId));
        }

        public List<Country> GetCountries(bool withPOIOnly)
        {
            if (withPOIOnly)
            {
                var poiCollection = Core.Data.CacheProviderMongoDB.DefaultInstance.GetPOICollection();
                //determine all countries with live POI
                var allPOICountries = (from cp in poiCollection.AsQueryable()
                                       where cp.SubmissionStatus.IsLive == true
                                       select cp.AddressInfo.CountryID).Distinct();

                return OCM.API.Common.Model.Extensions.Country.FromDataModel(dataModel.Countries.Where(c => allPOICountries.Contains(c.Id)).OrderBy(c => c.Title));
            }
            else
            {
                return OCM.API.Common.Model.Extensions.Country.FromDataModel(dataModel.Countries.OrderBy(c => c.Title));
            }
        }

        public List<OperatorInfo> GetOperators(int? countryId)
        {
            if (countryId != null)
            {
                return OCM.API.Common.Model.Extensions.OperatorInfo.FromDataModel(
                    dataModel.Operators.Where(c => c.ChargePoints.Any(cp => cp.AddressInfo.CountryId == countryId && cp.SubmissionStatusType.IsLive == true)).OrderBy(c => c.Title)
                    );
            }
            else
            {
                return OCM.API.Common.Model.Extensions.OperatorInfo.FromDataModel(dataModel.Operators.OrderBy(c => c.Title));
            }
        }

        public List<DataProvider> GetDataProviders()
        {
            var dataProviders = new List<Model.DataProvider>();
            foreach (var provider in dataModel.DataProviders.ToList())
            {
                dataProviders.Add(Model.Extensions.DataProvider.FromDataModel(provider));
            }
            return dataProviders;
        }

        public CoreReferenceData GetCoreReferenceData(APIRequestParams filter = null)
        {
            return GetCoreReferenceDataAsync(filter).Result;
        }

        public async Task<CoreReferenceData> GetCoreReferenceDataAsync(APIRequestParams filter = null)
        {
            if (filter == null) filter = new APIRequestParams { };

            CoreReferenceData data = null;

            if (filter.AllowMirrorDB)
            {
                // TODO: implement country filters for cached ref data
                data = await OCM.Core.Data.CacheManager.GetCoreReferenceData(filter);

                if (data != null) return data;
            }

            if (!filter.AllowDataStoreDB)
            {
                return null;
            }

            //can't get cached data, get fresh from database
            data = new CoreReferenceData();

            //list of Levels (ChargerTypes)
            data.ChargerTypes = [];

            await dataModel.ChargerTypes.ForEachAsync(cg => data.ChargerTypes.Add(Model.Extensions.ChargerType.FromDataModel(cg)));


            //list of connection types
            data.ConnectionTypes = new List<Model.ConnectionType>();

            if (filter.CountryIDs?.Any() == true)
            {
                // fetch connection types used in the list of given countries, with count of usage in the set
                var usedConnectionTypes = dataModel.ConnectionTypes
                    .Distinct()
                    .Where(c => c.ConnectionInfos.Any(ci => filter.CountryIDs.Contains(ci.ChargePoint.AddressInfo.CountryId) &&
                            (ci.ChargePoint.SubmissionStatusTypeId == (int)StandardSubmissionStatusTypes.Imported_Published
                                || ci.ChargePoint.SubmissionStatusTypeId == (int)StandardSubmissionStatusTypes.Submitted_Published)))
                    .Select(s => new
                    {
                        connectionType = s,
                        count = s.ConnectionInfos.Where(ci => filter.CountryIDs.Contains(ci.ChargePoint.AddressInfo.CountryId) &&
                        (ci.ChargePoint.SubmissionStatusTypeId == (int)StandardSubmissionStatusTypes.Imported_Published
                                || ci.ChargePoint.SubmissionStatusTypeId == (int)StandardSubmissionStatusTypes.Submitted_Published)).Count()
                    });

                await usedConnectionTypes
                    .Where(d => d.count > MINFILTER_CONNECTIONTYPE_INSTANCES)
                    .OrderBy(d => d.connectionType.Title).
                    ForEachAsync(ct => data.ConnectionTypes.Add(Model.Extensions.ConnectionType.FromDataModel(ct.connectionType)));

            }
            else
            {
                await dataModel.ConnectionTypes
                    .OrderBy(d => d.Title)
                    .ForEachAsync(ct => data.ConnectionTypes.Add(Model.Extensions.ConnectionType.FromDataModel(ct)));
            }

            //list of power source types (AC/DC etc)
            data.CurrentTypes = new List<Model.CurrentType>();
            await dataModel.CurrentTypes.ForEachAsync(ct => data.CurrentTypes.Add(Model.Extensions.CurrentType.FromDataModel(ct)));

            //list of countries
            data.Countries = new List<Model.Country>();

            await dataModel.Countries.ForEachAsync(country => data.Countries.Add(Model.Extensions.Country.FromDataModel(country)));

            //list of Data Providers
            data.DataProviders = new List<Model.DataProvider>();
            await dataModel.DataProviders.Include(d => d.DataProviderStatusType).ForEachAsync(provider => data.DataProviders.Add(Model.Extensions.DataProvider.FromDataModel(provider)));

            //list of Operators
            data.Operators = new List<Model.OperatorInfo>();

            if (filter.FilterOperatorsOnCountry && filter.CountryIDs?.Any() == true)
            {
                // fetch connection types used in the list of given countries, with count of usage in the set
                var usedNetworks = dataModel.Operators.Include(o => o.AddressInfo)
                    .Distinct()
                    .Where(c => c.ChargePoints
                                        .Where(cp => cp.SubmissionStatusTypeId == (int)StandardSubmissionStatusTypes.Imported_Published || cp.SubmissionStatusTypeId == (int)StandardSubmissionStatusTypes.Submitted_Published)
                                        .Any(poi => filter.CountryIDs.Contains(poi.AddressInfo.CountryId))
                                        )
                    .Select(o => new
                    {
                        operatorInfo = o,
                        count = o.ChargePoints.Where(poi =>
                                                        filter.CountryIDs.Contains(poi.AddressInfo.CountryId)
                                                        && (poi.SubmissionStatusTypeId == (int)StandardSubmissionStatusTypes.Imported_Published || poi.SubmissionStatusTypeId == (int)StandardSubmissionStatusTypes.Submitted_Published)
                                                        )
                        .Count()
                    });

                await usedNetworks
                    .Where(d => d.count > MINFILTER_OPERATOR_INSTANCES)
                    .OrderBy(d => d.operatorInfo.Title.Trim())
                    .ForEachAsync(ct => data.Operators.Add(Model.Extensions.OperatorInfo.FromDataModel(ct.operatorInfo)));

            }
            else
            {
                await dataModel.Operators.Include(o => o.AddressInfo).OrderBy(o => o.Title.Trim()).ForEachAsync(source => data.Operators.Add(Model.Extensions.OperatorInfo.FromDataModel(source)));
            }

            //list of Status Types
            data.StatusTypes = new List<Model.StatusType>();

            await dataModel.StatusTypes.ForEachAsync(status => data.StatusTypes.Add(Model.Extensions.StatusType.FromDataModel(status)));

            //list of Usage Types (public etc)
            data.UsageTypes = new List<Model.UsageType>();

            await dataModel.UsageTypes.OrderBy(u => u.Title).ForEachAsync(usage => data.UsageTypes.Add(Model.Extensions.UsageType.FromDataModel(usage)));

            //list of user comment types
            data.UserCommentTypes = new List<Model.UserCommentType>();
            await dataModel.UserCommentTypes.ForEachAsync(commentType => data.UserCommentTypes.Add(Model.Extensions.UserCommentType.FromDataModel(commentType)));

            //list of user comment types
            data.CheckinStatusTypes = new List<Model.CheckinStatusType>();
            await dataModel.CheckinStatusTypes.ForEachAsync(checkinType => data.CheckinStatusTypes.Add(Model.Extensions.CheckinStatusType.FromDataModel(checkinType)));

            data.SubmissionStatusTypes = new List<Model.SubmissionStatusType>();
            await dataModel.SubmissionStatusTypes.ForEachAsync(s => data.SubmissionStatusTypes.Add(Model.Extensions.SubmissionStatusType.FromDataModel(s)));

            data.MetadataGroups = new List<Model.MetadataGroup>();
            var groups = dataModel.MetadataGroups
                            .Include(m => m.MetadataFields)
                                .ThenInclude(f => f.DataType)
                            .Include(m => m.MetadataFields)
                                .ThenInclude(f => f.MetadataFieldOptions);

            await groups.ForEachAsync(g => data.MetadataGroups.Add(Model.Extensions.MetadataGroup.FromDataModel(g)));

            data.DataTypes = new List<Model.DataType>();
            await dataModel.DataTypes.ForEachAsync(d => data.DataTypes.Add(Model.Extensions.DataType.FromDataModel(d)));

            data.ChargePoint = new ChargePoint()
            {
                AddressInfo = new Model.AddressInfo(),
#pragma warning disable 612 //suppress obsolete warning
                Chargers = new List<Model.ChargerInfo> { new Model.ChargerInfo() },
#pragma warning restore 612
                Connections = new List<Model.ConnectionInfo> { new Model.ConnectionInfo() },
                DateCreated = DateTime.UtcNow,
                DateLastConfirmed = DateTime.UtcNow,
                DateLastStatusUpdate = DateTime.UtcNow,
                GeneralComments = "",
                DatePlanned = null,
                ID = -1,
                NumberOfPoints = 1,
                StatusType = new Model.StatusType(),
                OperatorInfo = new Model.OperatorInfo(),
                DataProvider = new Model.DataProvider(),
                UsageType = new Model.UsageType(),
                UUID = Guid.NewGuid().ToString(),
                DataQualityLevel = 1
            };

            data.UserComment = new Model.UserComment { ChargePointID = 0, Comment = "", CommentType = data.UserCommentTypes[0], DateCreated = DateTime.UtcNow, ID = 0, CheckinStatusType = data.CheckinStatusTypes[0] };
            return data;
        }
    }
}