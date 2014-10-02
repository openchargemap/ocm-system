using OCM.API.Common;
using System;
using System.Web.Mvc;

namespace OCM.MVC.Controllers
{
    public class BaseController : Controller
    {
        public bool IsRequestByRobot
        {
            get
            {
                try
                {
                    var userAgent = Request.UserAgent.ToLower();
                    if (userAgent.Contains("robot") || userAgent.Contains("crawler") || userAgent.Contains("spider") || userAgent.Contains("slurp") || userAgent.Contains("googlebot"))
                    {
                        return true;
                    }
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool IsUserSignedIn
        {
            get
            {
                if (Session["UserID"] != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns new or cached location guess based on client IP address
        /// </summary>
        /// <returns></returns>
        public LocationLookupResult PerformLocationGuess(bool includeCountryID)
        {
            LocationLookupResult locationGuess = null;

            if (Session["locationGuess"] != null)
            {
                locationGuess = (LocationLookupResult)Session["locationGuess"];
            }

            if (locationGuess == null || (locationGuess != null && (locationGuess.Country_Code == null || includeCountryID && locationGuess.CountryID == 0)))
            {
                var clientIP = Request.ServerVariables["REMOTE_ADDR"];

                locationGuess = GeocodingHelper.GetLocationFromIP_FreegeoIP(clientIP);

                if (includeCountryID)
                {
                    var country = new ReferenceDataManager().GetCountryByISO(locationGuess.Country_Code);
                    if (country != null)
                    {
                        locationGuess.CountryID = country.ID;
                    }
                }

                Session["locationGuess"] = locationGuess;
            }

            return locationGuess;
        }
    }
}