using System;
using System.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Session;
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
            if (IsReadOnlyMode) throw new Exception("Service is currently read-only.");
        }

        public ISession Session
        {
            get
            {
                return HttpContext.Session;
            }
        }

        public bool IsRequestByRobot
        {
            get
            {
                try
                {
                    var userAgent = Request.UserAgent().ToLower();
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

        public int?  UserID
        {
            get => HttpContext.Session.GetInt32("UserID");

            set
            {
                if (value != null)
                {
                    HttpContext.Session.SetInt32("UserID", (int)value);
                } else
                {
                    HttpContext.Session.Remove("UserID");
                }
            }
        }

        public bool IsUserSignedIn
        {
            get
            {
                if (HttpContext.Session.GetInt32("UserID") != null)
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
                return (HttpContext.Session.Get("IsAdministrator") != null && bool.Parse(HttpContext.Session.GetString("IsAdministrator")) == true);
            }
        }

        /// <summary>
        /// Returns new or cached location guess based on client IP address
        /// </summary>
        /// <returns></returns>
        public LocationLookupResult PerformLocationGuess(bool includeCountryID)
        {

            // FIXME:

            return null;
            /*
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
            */
        }
    }
}