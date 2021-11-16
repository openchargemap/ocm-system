﻿using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using OCM.API.Common.Model;
using OCM.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public async Task RefreshStats()
        {
            try
            {
                var paramList = new object[] { };

                await this.dataModel.Database.ExecuteSqlRawAsync("procUpdateOCMStats", paramList);
                AuditLogManager.Log(null, AuditEventType.StatisticsUpdated, "Statistics Updated", "");
            }
            catch (Exception)
            {
                ; ;
            }
        }

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
                    string isoCode = c.Isocode;
                    list.Add(new CountrySummary { CountryName = countryName, ISOCode = isoCode, ItemCount = item.NumItems, LocationCount = item.NumItems, StationCount = (int)item.NumStations, ItemType = "LocationsPerCountry" });
                }
            }
            else
            {
                //mongodb cache version of query
                var cacheDB = new OCM.Core.Data.CacheProviderMongoDB();
                var poiCollection = cacheDB.GetPOICollection();
            

                var results = poiCollection.AsQueryable()
                    .Where(c => c.SubmissionStatus.IsLive == true)
                    .GroupBy(c => c.AddressInfo.Country)
                    .Select(c => new { c.Key, NumItems = c.Count(), NumStations = c.Sum(s => s.NumberOfPoints > 0 ? s.NumberOfPoints : 1) });
                                          

                CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                TextInfo textInfo = cultureInfo.TextInfo;

                foreach (var item in results)
                {
                    var c = item.Key;
                    string countryName = textInfo.ToTitleCase(c.Title.ToLower());
                    string isoCode = c.ISOCode;
                    list.Add(new CountrySummary { CountryName = countryName, ISOCode = isoCode, ItemCount = item.NumItems, LocationCount = item.NumItems, StationCount = (int)item.NumStations, ItemType = "LocationsPerCountry" });
                }

            }

            return list;
        }

        public string GetTotalsPerCountrySummary(bool outputAsFunction, string functionName, APIRequestParams filterSettings)
        {
            //FIXME: move caching to caller
            string output = "function " + functionName + "() { var ocm_summary = new Array(); \r\n";

            var list = GetAllCountryStats();
            list.OrderByDescending(i => i.ItemCount).ToList();

            foreach (var item in list)
            {
                output += "ocm_summary[ocm_summary.length]={\"country\":\"" + item.CountryName + "\", \"isocode\":\"" + item.ISOCode + "\", \"itemcount\":" + item.LocationCount + ", \"locationcount\":" + item.LocationCount + ", \"stationcount\":" + item.StationCount + "}; \r\n";
            }
            output += " return ocm_summary; }";
            return output;
        }

        public async Task<POIRecentActivity> GetActivitySummary(APIRequestParams filterSettings)
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

            var allRecentPOIChanges = await poiManager.GetPOIListAsync(filterSettings);
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

        public GeneralStats GetStatSingle(string statTypeCode)
        {
            var stat = DataModel.Statistics.FirstOrDefault(s => s.StatTypeCode == statTypeCode);

            if (stat != null)
            {
                return new GeneralStats { Quantity = stat.NumItems };
            }
            else
            {
                return null;
            }
        }

        public List<GeneralStats> GetTopNStats(string statTypeCode, int maxResults, int? countryId)
        {
            var stats = DataModel.Statistics.Where(stat => stat.StatTypeCode == statTypeCode).OrderByDescending(s => s.NumItems).Take(maxResults).ToList();
            var results = new List<GeneralStats>();
            foreach (var s in stats)
            {
                if (s.UserId != null)
                {
                    var r = OCM.API.Common.Model.Extensions.User.PublicProfileFromDataModel(s.User, true);
                    if (r.EmailAddress != null) r.EmailHash = OCM.Core.Util.SecurityHelper.GetMd5Hash(r.EmailAddress);
                    results.Add(new GeneralStats { User = r, Quantity = s.NumItems });
                }
            }
            return results.OrderByDescending(r => r.Quantity).ToList();
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
            // TODO switch to datecreated based count from cache
            var stats = from p in DataModel.EditQueueItems
                        where p.DateSubmitted >= dateFrom && p.DateSubmitted <= dateTo && p.PreviousData == null
                        group p by new { month = p.DateSubmitted.Month, year = p.DateSubmitted.Year } into d
                        select new UserEditStats { Month = d.Key.month, Year = d.Key.year, NumberOfAdditions = d.Count() };

            var list = stats.Where(s => s.NumberOfAdditions > 1).OrderBy(s => s.Year).ThenBy(s => s.Month).ToList();

            if (list.Any()) list.RemoveAt(0); //remove first result as will only be partial month

            return list;
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