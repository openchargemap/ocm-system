using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using OCM.API.Common.Model;

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
        public int NumberOfComments {get;set;}
        public int NumberOfMediaItems {get;set;}
    }

    /// <summary>
    /// Provide basic summary counts/activity details on a per country basis or relative to specific filter parameters
    /// </summary>
    public class DataSummaryManager : ManagerBase
    {
        public string GetTotalsPerCountrySummary(bool outputAsFunction, string functionName, APIRequestSettings filterSettings)
        {
            //TODO: optionally output as normal JSON
            string output = "function " + functionName + "() { var ocm_summary = new Array(); \r\n";
            if (HttpContext.Current.Cache["ocm_summary"] == null)
            {
                var list = new List<CountrySummary>();

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

        public POIRecentActivity GetActivitySummary(APIRequestSettings filterSettings)
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

        public List<User> GetTopNContributors(int maxResults, int? countryId)
        {

            var contributors = DataModel.Users.Where(u => u.ID != (int)StandardUsers.System).OrderByDescending(u => u.ReputationPoints).Take(maxResults);

            var results = new List<User>();
            foreach (var u in contributors)
            {
                var r = OCM.API.Common.Model.Extensions.User.PublicProfileFromDataModel(u);
                if (u.EmailAddress != null) r.EmailHash = OCM.Core.Util.SecurityHelper.GetMd5Hash(u.EmailAddress);
                results.Add(r);
            }
            return results;
        }

        public List<GeneralStats> GetUserRegistrationStats(DateTime dateFrom, DateTime dateTo)
        {
            var stats = from p in DataModel.Users
                        where p.DateCreated >= dateFrom && p.DateCreated <= dateTo
                        group p by new { month = p.DateCreated.Month, year = p.DateCreated.Year } into d
                        select new GeneralStats { Month = d.Key.month, Year = d.Key.year, Quantity = d.Count() };

            return stats.OrderBy(s=>s.Year).ThenBy(s=>s.Month).ToList();
        }

        public List<UserEditStats> GetUserEditSummary(DateTime dateFrom, DateTime dateTo)
        {

            var stats = from p in DataModel.EditQueueItems
                        where p.DateSubmitted >= dateFrom && p.DateSubmitted <= dateTo && p.PreviousData== null
                        group p by new { month = p.DateSubmitted.Month, year = p.DateSubmitted.Year } into d
                        select new UserEditStats { Month = d.Key.month, Year = d.Key.year, NumberOfAdditions = d.Count() };

            return stats.OrderBy(s=>s.Year).ThenBy(s=>s.Month).ToList();
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