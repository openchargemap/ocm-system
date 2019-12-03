using OCM.API.Common;
using OCM.API.Common.DataSummary;
using OCM.MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace OCM.MVC.Controllers
{
    public class StatsController : Controller
    {
        // GET: Stats
        [ResponseCache(Duration = 60)] 
        public ActionResult Index()
        {
            var dataSummary = new DataSummaryManager();
            StatsModel model = new StatsModel();

            var dateTo = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddDays(-1);
            var dateFrom = dateTo.AddYears(-1);

            model.TopContributors = dataSummary.GetTopNStats("UserPOIChangesLast90Days", 10, null);
            model.TopCommentators = dataSummary.GetTopNStats("UserCommentsLast90Days", 10, null);
            model.TopMediaItems = dataSummary.GetTopNStats("UserMediaLast90Days", 10, null);
            model.UserRegistrations = dataSummary.GetUserRegistrationStats(dateFrom, dateTo);
            model.UserEdits = dataSummary.GetUserEditSummary(dateFrom, dateTo);
            model.UserComments = dataSummary.GetUserCommentStats(dateFrom, dateTo);

            model.TotalActiveContributors = dataSummary.GetStatSingle("TotalChangeContributorsLast90Days");
            model.TotalActiveEditors = dataSummary.GetStatSingle("ActiveEditorsLast90Days");
            model.TotalCommentContributors = dataSummary.GetStatSingle("TotalCommentContributorsLast90Days");
            model.TotalPhotoContributors = dataSummary.GetStatSingle("TotalPhotoContributorsLast90Days");

            var countryStats = dataSummary.GetAllCountryStats();
            model.TotalLocations = countryStats.Sum(c=>c.LocationCount);
            model.TotalStations = countryStats.Sum(c => c.StationCount);
            return View(model);
        }
    }
}