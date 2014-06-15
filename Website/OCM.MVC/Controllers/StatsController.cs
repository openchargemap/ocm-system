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
            model.TopContributors = dataSummary.GetTopNContributors(15, null);
            model.UserRegistrations = dataSummary.GetUserRegistrationStats(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow);
            model.UserEdits = dataSummary.GetUserEditSummary(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow);

            return View(model);
        }
    }
}