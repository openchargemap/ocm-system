using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.MVC.Models;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OCM.MVC.Controllers
{
    public class ProfileController : BaseController
    {
        protected static void UpdateCookie(HttpResponseBase response, string cookieName, string cookieValue)
        {
            response.Cookies[cookieName].Value = cookieValue;
        }

        protected static void ClearCookie(HttpResponseBase response, string cookieName, string cookieValue)
        {
            if (response.Cookies.AllKeys.Contains(cookieName))
            {
                response.Cookies[cookieName].Value = cookieValue;
                response.Cookies[cookieName].Expires = DateTime.UtcNow.AddDays(-1);
            }
        }

        //
        // GET: /Profile/

        [AuthSignedInOnly]
        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                UserManager userManager = new UserManager();
                var user = userManager.GetUser(int.Parse(Session["UserID"].ToString()));

                return View(user);
            }
            return View();
        }

        public ActionResult SignIn(string redirectUrl)
        {
            return RedirectToAction("Index", "LoginProvider",
                                    new { _mode = "silent", _forceLogin = true, _redirectUrl = redirectUrl });
        }

        public ActionResult SignOut()
        {
            if (Session["UserID"] != null)
            {
                // assign fresh session token for next login
                var userManager = new UserManager();
                userManager.AssignNewSessionToken((int)Session["UserID"]);
            }

            //clear cookies & set new session token
            UpdateCookie(Response, "IdentityProvider", "");
            UpdateCookie(Response, "Identifier", "");
            UpdateCookie(Response, "Username", "");
            UpdateCookie(Response, "OCMSessionToken", "");
            UpdateCookie(Response, "AccessPermissions", "");

            //clear session
            Session.Abandon();

            return RedirectToAction("Index", "Home");
        }

        [AuthSignedInOnly]
        public ActionResult View(int id)
        {
            UserManager userManager = new UserManager();
            var user = userManager.GetUser(id);

            ViewBag.CountryList = new SelectList(new ReferenceDataManager().GetCountries(true), "ID", "Title");
            return View(user);
        }

        [AuthSignedInOnly]
        public ActionResult Edit()
        {
            if (Session["UserID"] != null)
            {
                UserManager userManager = new UserManager();
                var user = userManager.GetUser(int.Parse(Session["UserID"].ToString()));

                return View(user);
            }
            else return View();
        }

        [AuthSignedInOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User updateProfile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (Session["UserID"] != null)
                    {
                        // TODO: Add update logic here
                        var userManager = new UserManager();
                        var user = userManager.GetUser((int)Session["UserID"]);

                        if (user.ID == updateProfile.ID)
                        {
                            userManager.UpdateUserProfile(updateProfile, false);
                        }
                        return RedirectToAction("Index");
                    }
                    else return View();
                }
                catch
                {
                    return View();
                }
            }
            return View(updateProfile);
        }

        [AuthSignedInOnly]
        public ActionResult Comments()
        {
            UserManager userManager = new UserManager();

            var user = userManager.GetUser(int.Parse(Session["UserID"].ToString()));
            var list = new UserCommentManager().GetUserComments(user.ID);
            return View(list);
        }

        [AuthSignedInOnly]
        public ActionResult CommentDelete(int id)
        {
            var user = new UserManager().GetUser(int.Parse(Session["UserID"].ToString()));
            var commentManager = new UserCommentManager();
            var list = commentManager.GetUserComments(user.ID);

            //delete comment if owned by this user
            if (list.Where(c => c.User.ID == user.ID && c.ID == id).Any())
            {
                commentManager.DeleteComment(user.ID, id);
            }

            return RedirectToAction("Comments");
        }

        [AuthSignedInOnly]
        public ActionResult Media()
        {
            UserManager userManager = new UserManager();

            var user = userManager.GetUser(int.Parse(Session["UserID"].ToString()));
            var list = new MediaItemManager().GetUserMediaItems(user.ID);
            return View(list);
        }

        [AuthSignedInOnly]
        public ActionResult MediaDelete(int id)
        {
            var user = new UserManager().GetUser(int.Parse(Session["UserID"].ToString()));
            var itemManager = new MediaItemManager();
            var list = itemManager.GetUserMediaItems(user.ID);

            //delete item if owned by this user
            if (list.Where(c => c.User.ID == user.ID && c.ID == id).Any())
            {
                itemManager.DeleteMediaItem(user.ID, id);
            }

            return RedirectToAction("Media");
        }

        [AuthSignedInOnly]
        public ActionResult Subscriptions()
        {
            UserManager userManager = new UserManager();

            var user = userManager.GetUser(int.Parse(Session["UserID"].ToString()));
            ViewBag.UserProfile = user;

            ViewBag.ReferenceData = new ReferenceDataManager().GetCoreReferenceData();

            var list = new UserSubscriptionManager().GetUserSubscriptions(user.ID);
            return View(list);
        }

        [AuthSignedInOnly]
        public ActionResult SubscriptionEdit(int? id)
        {
            var subscription = new UserSubscription();
            var userId = int.Parse(Session["UserID"].ToString());
            if (id != null)
            {
                subscription = new UserSubscriptionManager().GetUserSubscription(userId, (int)id);
            }
            else
            {
                LocationLookupResult locationGuess = PerformLocationGuess(true);

                subscription.UserID = userId;
                subscription.NotificationFrequencyMins = 60 * 24 * 7;//default to 1 week
                subscription.DistanceKM = 100;

                if (locationGuess != null && locationGuess.Country_Code != null)
                {
                    subscription.CountryID = locationGuess.CountryID;
                    subscription.Country = new Country { ID = (int)locationGuess.CountryID, Title = locationGuess.Country_Name, ISOCode = locationGuess.Country_Code };
                }
                else
                {
                    //default to UK
                    subscription.CountryID = 1;
                    subscription.Country = new ReferenceDataManager().GetCountryByISO("GB");
                }
                subscription.Latitude = null;
                subscription.Longitude = null;
                subscription.IsEnabled = true;
                subscription.NotifyComments = true;
                subscription.NotifyPOIAdditions = true;
                subscription.NotifyPOIUpdates = true;

                if (locationGuess != null && locationGuess.SuccessfulLookup)
                {
                    if (locationGuess.CountryID != null) subscription.CountryID = locationGuess.CountryID;
                    if (locationGuess.Latitude != null && locationGuess.Longitude != null)
                    {
                        subscription.Latitude = locationGuess.Latitude;
                        subscription.Longitude = locationGuess.Longitude;
                    }
                }

                subscription.Title = "Charging Locations Near Me";
            }

            PopulateSubscriptionEditorViewBag(subscription);
            return View("SubscriptionEdit", subscription);
        }

        private void PopulateSubscriptionEditorViewBag(UserSubscription subscription)
        {
            var refDataManager = new ReferenceDataManager();
            var coreRefData = refDataManager.GetCoreReferenceData();

            var allCountries = coreRefData.Countries;

            allCountries.Insert(0, new Country { ID = 0, ISOCode = "", Title = "(All Countries)" });

            ViewBag.CountryList = new SelectList(allCountries, "ISOCode", "Title", (subscription.Country != null ? subscription.Country.ISOCode : null));

            var NotificationFrequencyOptions = new[]{
                new { ID = 5, Title = "Every 5 Minutes"},
                new { ID = 30, Title = "Every 30 Minutes"},
                new { ID = 60, Title = "Every Hour"},
                new { ID = 60*12, Title = "Every 12 Hours"},
                new { ID = 60*24, Title = "Every Day"},
                new { ID = 60*24*7, Title = "Every Week"},
                new { ID = 60*24*7*31, Title = "Every Month"},
            };

            ViewBag.NotificationFrequencyOptions = new SelectList(NotificationFrequencyOptions, "ID", "Title", subscription.NotificationFrequencyMins);

            //var matchingItems = new UserSubscriptionManager().GetAllSubscriptionMatches();
            //ViewBag.MatchingItems = matchingItems;

            ViewBag.CountryExtendedInfo = JsonConvert.SerializeObject(OCM.Core.Data.CacheManager.GetExtendedCountryInfo());

            if (subscription.FilterSettings == null) subscription.FilterSettings = new UserSubscriptionFilter();
            ViewBag.OperatorList = new MultiSelectList(refDataManager.GetOperators(subscription.CountryID), "ID", "Title", subscription.FilterSettings.OperatorIDs);
            ViewBag.LevelList = new MultiSelectList(coreRefData.ChargerTypes, "ID", "Title", subscription.FilterSettings.LevelIDs);
            ViewBag.ConnectionTypeList = new MultiSelectList(coreRefData.ConnectionTypes, "ID", "Title", subscription.FilterSettings.ConnectionTypeIDs);
            ViewBag.StatusTypeList = new MultiSelectList(coreRefData.StatusTypes, "ID", "Title", subscription.FilterSettings.StatusTypeIDs);
            ViewBag.UsageTypeList = new MultiSelectList(coreRefData.UsageTypes, "ID", "Title", subscription.FilterSettings.UsageTypeIDs);
        }

        [AuthSignedInOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubscriptionEdit(UserSubscription userSubscription)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(Request["CountryCode"]))
                {
                    //TEMP fix country id passed as ISO
                    var country = new ReferenceDataManager().GetCountryByISO(Request["CountryCode"]);
                    userSubscription.CountryID = country.ID;
                }
                else
                {
                    userSubscription.Country = null;
                    userSubscription.CountryID = null;
                }
                userSubscription = new UserSubscriptionManager().UpdateUserSubscription(userSubscription);
                return RedirectToAction("Subscriptions", "Profile");
            }

            PopulateSubscriptionEditorViewBag(userSubscription);
            return View(userSubscription);
        }

        private SubscriptionBrowseModel PrepareSubscriptionBrowseModel(SubscriptionBrowseModel model)
        {
            if (model.DateFrom == null) model.DateFrom = DateTime.UtcNow.AddDays(-30);
            else
            {
                //enforce min date from
                if (model.DateFrom < DateTime.UtcNow.AddYears(-1))
                {
                    model.DateFrom = DateTime.UtcNow.AddYears(-1);
                }
            }
            var subscriptionManager = new UserSubscriptionManager();
            var userId = int.Parse(Session["UserID"].ToString());
            model.SubscriptionResults = subscriptionManager.GetSubscriptionMatches((int)model.SubscriptionID, userId, dateFrom: model.DateFrom);
            model.SummaryHTML = subscriptionManager.GetSubscriptionMatchHTMLSummary(model.SubscriptionResults);
            model.Subscription = subscriptionManager.GetUserSubscription(userId, (int)model.SubscriptionID);
            return model;
        }

        [AuthSignedInOnly]
        public ActionResult SubscriptionMatches(SubscriptionBrowseModel model)
        {
            if (model.SubscriptionID != null)
            {
                model = PrepareSubscriptionBrowseModel(model);
            }
            return View(model);
        }

        [AuthSignedInOnly]
        public ActionResult SubscriptionDelete(int id)
        {
            var subscriptionManager = new UserSubscriptionManager();
            var userId = int.Parse(Session["UserID"].ToString());
            subscriptionManager.DeleteSubscription(userId, id);
            return RedirectToAction("Subscriptions");
        }
    }
}