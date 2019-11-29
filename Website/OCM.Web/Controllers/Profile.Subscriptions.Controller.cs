using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.MVC.Models;

namespace OCM.MVC.Controllers
{
    public partial class ProfileController : BaseController
    {

        [Authorize(Roles = "StandardUser")]
        public ActionResult Subscriptions()
        {
            UserManager userManager = new UserManager();

            var user = userManager.GetUser((int)UserID);
            ViewBag.UserProfile = user;

            ViewBag.ReferenceData = new ReferenceDataManager().GetCoreReferenceData(new APIRequestParams());

            var list = new UserSubscriptionManager().GetUserSubscriptions(user.ID);
            return View(list);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult SubscriptionEdit(int? id)
        {
            var subscription = new UserSubscription();
            var userId = (int)UserID;

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
                    using (var refDataManager = new ReferenceDataManager())
                    {
                        subscription.Country = refDataManager.GetCountryByISO("GB");
                    }
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
            using (var refDataManager = new ReferenceDataManager())
            {
                var coreRefData = refDataManager.GetCoreReferenceData(new APIRequestParams());

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

                ViewBag.CountryExtendedInfo = JsonConvert.SerializeObject(Core.Data.CacheManager.GetExtendedCountryInfo());

                if (subscription.FilterSettings == null) subscription.FilterSettings = new UserSubscriptionFilter();
                ViewBag.OperatorList = new MultiSelectList(refDataManager.GetOperators(subscription.CountryID), "ID", "Title", subscription.FilterSettings.OperatorIDs);
                ViewBag.LevelList = new MultiSelectList(coreRefData.ChargerTypes, "ID", "Title", subscription.FilterSettings.LevelIDs);
                ViewBag.ConnectionTypeList = new MultiSelectList(coreRefData.ConnectionTypes, "ID", "Title", subscription.FilterSettings.ConnectionTypeIDs);
                ViewBag.StatusTypeList = new MultiSelectList(coreRefData.StatusTypes, "ID", "Title", subscription.FilterSettings.StatusTypeIDs);
                ViewBag.UsageTypeList = new MultiSelectList(coreRefData.UsageTypes, "ID", "Title", subscription.FilterSettings.UsageTypeIDs);
            }
        }

        [Authorize(Roles = "StandardUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubscriptionEdit(UserSubscription userSubscription)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(Request.Form["CountryCode"]))
                {
                    //TEMP fix country id passed as ISO
                    using (var refDataManager = new ReferenceDataManager())
                    {
                        var country = refDataManager.GetCountryByISO(Request.Form["CountryCode"]);
                        userSubscription.CountryID = country.ID;
                    }
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

            model.SubscriptionResults = subscriptionManager.GetSubscriptionMatches((int)model.SubscriptionID, (int)UserID, dateFrom: model.DateFrom);
            model.SummaryHTML = subscriptionManager.GetSubscriptionMatchHTMLSummary(model.SubscriptionResults);
            model.Subscription = subscriptionManager.GetUserSubscription((int)UserID, (int)model.SubscriptionID);
            return model;
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult SubscriptionMatches(SubscriptionBrowseModel model)
        {
            if (model.SubscriptionID != null)
            {
                model = PrepareSubscriptionBrowseModel(model);
            }
            return View(model);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult SubscriptionDelete(int id)
        {
            var subscriptionManager = new UserSubscriptionManager();

            subscriptionManager.DeleteSubscription((int)UserID, id);
            return RedirectToAction("Subscriptions");
        }
    }
}