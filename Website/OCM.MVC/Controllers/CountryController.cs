using OCM.API.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OCM.MVC.Controllers
{
    public class CountryController : Controller
    {
        // GET: Country and /Country/UnitedKingdom
        [OutputCache(Duration = 60, VaryByParam = "countryname")]
        public ActionResult Index(string countryname)
        {
            if (!String.IsNullOrEmpty(countryname))
            {
                
                var country = new ReferenceDataManager().GetCountryByName(countryname);
                return this.Summary(countryname);
            }
            
            //list of all countries
            var allCountries = new ReferenceDataManager().GetCountries(true);

            var continents = new Dictionary<string, string>();
            continents.Add("AS", "Asia");
            continents.Add("EU", "Europe");
            continents.Add("NA", "North America");
            continents.Add("OC", "Oceania");
            continents.Add("SA", "South America");

            ViewBag.Continents = continents;

            return View(allCountries);
        }

        public ActionResult Summary(string countryname)
        {

            return View();
        }

        [Route("country/{countryname}/networks")]
        [OutputCache(Duration = 60, VaryByParam = "countryname")]
        public ActionResult Networks(string countryname)
        {
            var country = new ReferenceDataManager().GetCountryByName(countryname);
            if (country != null)
            {
                //int[] genericOperatorIds = { 1, 44, 45 };
                var operators = new ReferenceDataManager().GetOperators(country.ID).Where(o=>o.ID!=1 && o.ID!=44 && o.ID!=45).ToList();
                ViewBag.Country = country;
                return View(operators);
            }
            return View();
        }
    }
}