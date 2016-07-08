using System;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using OCM.API.Common;

namespace OCM.MVC.Controllers
{
    public class BaseController : Controller
    {
        public bool IsReadOnlyMode
        {
            get
            {
                if (!bool.Parse(ConfigurationManager.AppSettings["EnableDataWrites"]))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void CheckForReadOnly()
        {
            if (IsReadOnlyMode) throw new HttpException(404, "Service is currently read-only.");
        }

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

        public bool IsUserAdmin
        {
            get
            {
                return (Session["IsAdministrator"] != null && (bool)Session["IsAdministrator"] == true);
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