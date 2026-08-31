using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OCM.API.Common;
using OCM.Core.Settings;
using System;

namespace OCM.MVC.Controllers
{
    public class BaseController : Controller
    {

        public static CoreSettings GetSettingsFromConfig(IConfiguration config)
        {
            var settings = new CoreSettings();
            config.GetSection("CoreSettings").Bind(settings);
            return settings;
        }

        public bool IsReadOnlyMode
        {
            get
            {
                if (!bool.Parse(System.Configuration.ConfigurationManager.AppSettings["EnableDataWrites"]))
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
                    if (
                        userAgent.Contains("robot")
                        || userAgent.Contains("crawler")
                        || userAgent.Contains("spider")
                        || userAgent.Contains("slurp")
                        || userAgent.Contains("googlebot")
                        || userAgent.Contains("SEOkicks")
                        || userAgent.Contains("DotBot")
                        || userAgent.Contains("bingbot")
                        || userAgent.Contains("AhrefsBot")
                        || userAgent.Contains("SemrushBot")
                        || userAgent.Contains("MJ12bot")
                        )
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

        public int? UserID
        {
            get => HttpContext.Session.GetInt32("UserID");

            set
            {
                if (value != null)
                {
                    HttpContext.Session.SetInt32("UserID", (int)value);
                }
                else
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
        /// Get the full profile of the currently signed in user, or null if the user is not signed in
        /// </summary>
        /// <returns></returns>
        public OCM.API.Common.Model.User GetCurrentUserProfile()
        {
            if (!IsUserSignedIn) return null;

            return new UserManager().GetUser((int)UserID);
        }

        /// <summary>
        /// True if an administrator has blocked the given user from contributing edits, comments and media
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsUserEditingBlocked(OCM.API.Common.Model.User user)
        {
            return UserManager.IsUserEditingBlocked(user);
        }

        /// <summary>
        /// Standard response shown to a user who has been blocked from editing, explaining how they can request that their edit permissions are reinstated
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public ViewResult EditingBlockedView(OCM.API.Common.Model.User user)
        {
            Response.StatusCode = 403;

            ViewBag.EditingBlockedReason = UserManager.GetEditingBlockedReason(user);

            return View("EditingBlocked");
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