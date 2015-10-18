using MongoDB.Bson;
using OCM.API.Common.Model;
using OCM.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;

namespace OCM.API.Common.DataSummary
{
    public class CountrySummary
    {
        public string CountryName { get; set; }

        public string ISOCode { get; set; }

        public int ItemCount { get; set; }

        public int LocationCount { get; set; }

        public int StationCount { get; set; }

        public string ItemType { get; set; }
    }

    public class GeneralStats
    {
        public User User { get; set; }

        public Country Country { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public int Quantity { get; set; }
    }

    public class UserEditStats
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public int NumberOfEdits { get; set; }

        public int NumberOfAdditions { get; set; }

        public int NumberOfComments { get; set; }

        public int NumberOfMediaItems { get; set; }
    }

    /// <summary>
    /// Provide basic summary counts/activity details on a per country basis or relative to specific filter parameters
    /// </summary>
    public class DataSummaryManager : ManagerBase
    {
        public List<CountrySummary> GetAllCountryStats()
        {
            var list = new List<CountrySummary>();
            bool sqlDatabaseQueryMode = false;
            if (sqlDatabaseQueryMode)
            {
                var results = from c in DataModel.ChargePoints
                              where c.SubmissionStatusType.IsLive == true
                              group c by c.AddressInfo.Country into g
                              select new { g, NumItems = g.Count(), NumStations = g.Sum(s => s.NumberOfPoints > 0 ? s.NumberOfPoints : 1) };

                CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                TextInfo textInfo = cultureInfo.TextInfo;

                foreach (var item in results)
                {
                    var c = item.g.First().AddressInfo.Country;
                    string countryName = textInfo.ToTitleCase(c.Title.ToLower());
                    string isoCode = c.ISOCode;
                    list.Add(new CountrySummary { CountryName = countryName, ISOCode = isoCode, ItemCount = item.NumItems, LocationCount = item.NumItems, StationCount = (int)item.NumStations, ItemType = "LocationsPerCountry" });
                }
            }
            else
            {
                //mongodb cache version of query

                var match = new BsonDocument
                {
                    {
                        "$match",
                        new BsonDocument
                            {
                                {"SubmissionStatus.IsLive", true}
                            }
                    }
                };

                var group = new BsonDocument
                {
                    { "$group",
                        new BsonDocument
                            {
                                { "_id", new BsonDocument {
                                                 {
                                                     "Country","$AddressInfo.Country"
                                                 }
                                             }
                                },
                                {
                                    "POICount", new BsonDocument
                                                 {
                                                     {
                                                         "$sum", 1
                                                     }
                                                 }
                                },
                               {
                                    "StationCount", new BsonDocument
                                                 {
                                                     {
                                                        "$sum",   new BsonDocument{
                                                        {"$ifNull",
                                                            new BsonArray{
                                                            "$NumberOfPoints",
                                                            1
                                                            }
                                                        }
                                                         }
                                                     }
                                                 }
                                }
                            }
                  }
                };

                var project = new BsonDocument
                    {
                        {
                            "$project",
                            new BsonDocument
                            {
                                {"_id", 0},
                                {"Country","$_id.Country"},
                                {"POICount", 1},
                                {"StationCount",
                                  1
                                }
                            }
                        }
                    };

                var pipeline = new[] { match, group, project };

                OCM.Core.Data.CacheProviderMongoDB cacheDB = new OCM.Core.Data.CacheProviderMongoDB();
                var poiCollection = cacheDB.GetPOICollection();
                var result = poiCollection.Aggregate(new MongoDB.Driver.AggregateArgs { Pipeline = pipeline });

                var results = result
                    .Select(x => x.ToDynamic())
                .ToList();

                foreach (var item in results)
                {
                    list.Add(new CountrySummary { CountryName = item.Country.Title, ISOCode = item.Country.ISOCode, ItemCount = item.POICount, LocationCount = item.POICount, StationCount = (int)item.StationCount, ItemType = "LocationsPerCountry" });
                }
            }

            return list;
        }

        public string GetTotalsPerCountrySummary(bool outputAsFunction, string functionName, APIRequestParams filterSettings)
        {
            //TODO: optionally output as normal JSON
            string output = "function " + functionName + "() { var ocm_summary = new Array(); \r\n";

            if (HttpContext.Current.Cache["ocm_summary"] == null)
            {
                var list = GetAllCountryStats();
                HttpContext.Current.Cache["ocm_summary"] = list.OrderByDescending(i => i.ItemCount).ToList();
            }
            var cachedresults = (List<CountrySummary>)HttpContext.Current.Cache["ocm_summary"];
            foreach (var item in cachedresults)
            {
                output += "ocm_summary[ocm_summary.length]={\"country\":\"" + item.CountryName + "\", \"isocode\":\"" + item.ISOCode + "\", \"itemcount\":" + item.LocationCount + ", \"locationcount\":" + item.LocationCount + ", \"stationcount\":" + item.StationCount + "}; \r\n";
            }
            output += " return ocm_summary; }";
            return output;
        }

        public POIRecentActivity GetActivitySummary(APIRequestParams filterSettings)
        {
            //default to last month
            DateTime dateFrom = DateTime.UtcNow.AddMonths(-1);

            if (filterSettings != null)
            {
                if (filterSettings.ChangesFromDate != null)
                {
                    dateFrom = filterSettings.ChangesFromDate.Value;
                }
                else
                {
                    filterSettings.ChangesFromDate = dateFrom;
                }
            }

            //populate recently added comments
            var recentComments = from c in DataModel.UserComments
                                 where c.DateCreated >= dateFrom
                                 select c;

            var summary = new POIRecentActivity();
            summary.RecentComments = new List<UserComment>();
            foreach (var c in recentComments.OrderByDescending(c => c.DateCreated).Take(10).ToList())
            {
                summary.RecentComments.Add(Model.Extensions.UserComment.FromDataModel(c, true));
            }

            //populate recently modified charge points TODO: differentiate between updated since and created since?
            var poiManager = new POIManager();

            var allRecentPOIChanges = poiManager.GetChargePoints(filterSettings);
            summary.POIRecentlyAdded = allRecentPOIChanges.Where(p => p.DateCreated >= dateFrom).OrderByDescending(p => p.DateCreated).Take(10).ToList();
            summary.POIRecentlyUpdated = allRecentPOIChanges.Where(p => p.DateLastStatusUpdate >= dateFrom && p.DateCreated != p.DateLastStatusUpdate).OrderByDescending(p => p.DateLastStatusUpdate).Take(10).ToList();

            var recentMedia = DataModel.MediaItems.Where(m => m.DateCreated > dateFrom && m.IsEnabled == true).OrderByDescending(m => m.DateCreated).Take(10).ToList();
            summary.RecentMedia = new List<MediaItem>();
            foreach (var mediaItem in recentMedia.OrderByDescending(m => m.DateCreated))
            {
                summary.RecentMedia.Add(Model.Extensions.MediaItem.FromDataModel(mediaItem));
            }

            return summary;
        }

        public List<GeneralStats> GetTopNStats(string statTypeCode, int maxResults, int? countryId)
        {
            var stats = DataModel.Statistics.Where(stat => stat.StatTypeCode == statTypeCode).OrderByDescending(s => s.NumItems).Distinct().Take(maxResults);
            var results = new List<GeneralStats>();
            foreach (var s in stats)
            {
                if (s.UserID != null)
                {
                    var r = OCM.API.Common.Model.Extensions.User.PublicProfileFromDataModel(s.User);
                    if (r.EmailAddress != null) r.EmailHash = OCM.Core.Util.SecurityHelper.GetMd5Hash(r.EmailAddress);
                    results.Add(new GeneralStats { User = r, Quantity = s.NumItems });
                }
            }
            return results;
        }

        public List<GeneralStats> GetUserRegistrationStats(DateTime dateFrom, DateTime dateTo)
        {
            var stats = from p in DataModel.Users
                        where p.DateCreated >= dateFrom && p.DateCreated <= dateTo
                        group p by new { month = p.DateCreated.Month, year = p.DateCreated.Year } into d
                        select new GeneralStats { Month = d.Key.month, Year = d.Key.year, Quantity = d.Count() };

            return stats.OrderBy(s => s.Year).ThenBy(s => s.Month).ToList();
        }

        public List<UserEditStats> GetUserEditSummary(DateTime dateFrom, DateTime dateTo)
        {
            var stats = from p in DataModel.EditQueueItems
                        where p.DateSubmitted >= dateFrom && p.DateSubmitted <= dateTo && p.PreviousData == null
                        group p by new { month = p.DateSubmitted.Month, year = p.DateSubmitted.Year } into d
                        select new UserEditStats { Month = d.Key.month, Year = d.Key.year, NumberOfAdditions = d.Count() };

            return stats.OrderBy(s => s.Year).ThenBy(s => s.Month).ToList();
        }

        public List<GeneralStats> GetUserCommentStats(DateTime dateFrom, DateTime dateTo)
        {
            var stats = from p in DataModel.UserComments
                        where p.DateCreated >= dateFrom && p.DateCreated <= dateTo
                        group p by new { month = p.DateCreated.Month, year = p.DateCreated.Year } into d
                        select new GeneralStats { Month = d.Key.month, Year = d.Key.year, Quantity = d.Count() };

            return stats.OrderBy(s => s.Year).ThenBy(s => s.Month).ToList();
        }
    }
}