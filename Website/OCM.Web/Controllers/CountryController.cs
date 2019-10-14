using Microsoft.AspNetCore.Mvc;
using OCM.API.Common;
using OCM.API.Common.DataSummary;
using System;
using System.Collections.Generic;
using System.Linq;


namespace OCM.MVC.Controllers
{
    public class CountryController : Controller
    {
        // GET: Country and /Country/UnitedKingdom
        //[OutputCache(Duration = 60, VaryByParam = "countryname")]
        public ActionResult Index(string countryname)
        {
            using (var refDataManager = new ReferenceDataManager())
            {

                if (!String.IsNullOrEmpty(countryname))
                {

                    var country = refDataManager.GetCountryByName(countryname);
                    return this.Summary(countryname);
                }

                //list of all countries
                var allCountries = refDataManager.GetCountries(false);
                using (var dataSummaryManager = new DataSummaryManager())
                {
                    var countryStats = dataSummaryManager.GetAllCountryStats();

                    var continents = new Dictionary<string, string>();
                    continents.Add("AS", "Asia");
                    continents.Add("EU", "Europe");
                    continents.Add("NA", "North America");
                    continents.Add("OC", "Oceania");
                    continents.Add("SA", "South America");
                    continents.Add("AF", "Africa");

                    ViewBag.Continents = continents;
                    ViewBag.CountryStats = countryStats;
                }
                return View(allCountries);
            }
        }

        public ActionResult Summary(string countryname)
        {

            return View();
        }

        [Route("country/{countryname}/networks")]
        //[OutputCache(Duration = 60, VaryByParam = "countryname")]
        public ActionResult Networks(string countryname)
        {
            using (var refDataManager = new ReferenceDataManager())
            {
                var country = refDataManager.GetCountryByName(countryname);
                if (country != null)
                {
                    //int[] genericOperatorIds = { 1, 44, 45 };
                    var operators = refDataManager.GetOperators(country.ID).Where(o => o.ID != 1 && o.ID != 44 && o.ID != 45).ToList();
                    ViewBag.Country = country;
                    return View(operators);
                }
                return View();
            }
        }
    }
}
