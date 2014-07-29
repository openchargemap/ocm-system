using OCM.API.Common;
using OCM.API.Common.DataSummary;
using OCM.MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OCM.MVC.Controllers
{
    
    public class StatsController : Controller
    {
        // GET: Stats
        [OutputCache(Duration = 60)]
        public ActionResult Index()
        {
            var dataSummary = new DataSummaryManager();
            StatsModel model = new StatsModel();
            
            var dateTo = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddDays(-1);
            var dateFrom = dateTo.AddYears(-1);

            model.TopContributors = dataSummary.GetTopNContributors(15, null);
            model.UserRegistrations = dataSummary.GetUserRegistrationStats(dateFrom, dateTo);
            model.UserEdits = dataSummary.GetUserEditSummary(dateFrom, dateTo);
            model.UserComments = dataSummary.GetUserCommentStats(dateFrom, dateTo);

            return View(model);
        }
    }
}