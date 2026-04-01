using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.Web.Models;
using OCM.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.MVC.Controllers
{
    public partial class ProfileController : BaseController
    {
        public static void UpdateCookie(HttpContext context, string cookieName, string cookieValue)
        {
            if (context.Request.Cookies.Keys.Contains(cookieName))
            {
                context.Response.Cookies.Delete(cookieName);
            }

            context.Response.Cookies.Append(cookieName, cookieValue, new CookieOptions { Expires = DateTime.Now.AddMonths(2) });
        }

        protected static void ClearCookie(HttpResponse response, string cookieName, string cookieValue)
        {
            response.Cookies.Delete(cookieName);
        }

        //
        // GET: /Profile/

        [Authorize(Roles = "StandardUser")]
        public ActionResult Index()
        {
            var userManager = new UserManager();
            var user = userManager.GetUser((int)UserID);

            return View(user);
        }

        public ActionResult SignIn(string redirectUrl)
        {
            return RedirectToAction("Index", "LoginProvider",
                                    new { _mode = "silent", _forceLogin = true, _redirectUrl = redirectUrl });
        }

        public ActionResult SignOut()
        {
            if (IsUserSignedIn)
            {
                // FIXME: need a token for app logins vs web logins as signout of web shouldn't be a sign out of app
                // assign fresh session token for next login
                var userManager = new UserManager();
                userManager.AssignNewSessionToken((int)UserID);
            }

            //clear cookies
            foreach (var cookieKey in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookieKey);
            }

            //clear session
            Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult View(int id)
        {
            var userManager = new UserManager();
            var user = userManager.GetUser(id);
            using (var refDataManager = new ReferenceDataManager())
            {
                ViewBag.CountryList = new SelectList(refDataManager.GetCountries(false), "ID", "Title");
            }
            return View(user);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult Edit()
        {
            var userManager = new UserManager();
            var user = userManager.GetUser((int)UserID);

            return View(user);
        }

        [Authorize(Roles = "StandardUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User updateProfile)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    var userManager = new UserManager();
                    var user = userManager.GetUser((int)UserID);

                    bool updatedOK = false;
                    if (user.ID == updateProfile.ID)
                    {
                        updatedOK = userManager.UpdateUserProfile(updateProfile, false);
                    }

                    if (updatedOK)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["UpdateFailed"] = true;
                    }

                }
                catch
                {
                    return View();
                }
            }
            return View(updateProfile);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult ChangePassword()
        {
            bool requireCurrentPassword = true;

            var userManager = new UserManager();
            //allow user to set a new password without confirming old one if they haven't set a password yet
            if (!userManager.HasPassword((int)UserID))
            {
                requireCurrentPassword = false;
            }

            if (TempData["IsCurrentPasswordRequired"] != null && (bool)TempData["IsCurrentPasswordRequired"] == false)
            {
                requireCurrentPassword = false;
            }
            return View(new PasswordChangeModel { IsCurrentPasswordRequired = requireCurrentPassword });
        }

        [Authorize(Roles = "StandardUser"), HttpPost, ValidateAntiForgeryToken]
        public ActionResult ChangePassword(API.Common.Model.PasswordChangeModel model)
        {
            if (ModelState.IsValid)
            {
                var passwordChanged = new UserManager().SetNewPassword((int)UserID, model);

                model.PasswordResetFailed = !passwordChanged;

                if (passwordChanged)
                {
                    model.PasswordResetCompleted = true;
                    return RedirectToAction("Index", "Profile");
                }
            }

            return View(model);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult Comments()
        {
            UserManager userManager = new UserManager();

            var user = userManager.GetUser((int)UserID);
            using (var commentManager = new UserCommentManager())
            {
                var list = commentManager.GetUserComments(user.ID).OrderByDescending(c => c.DateCreated);
                return View(list);
            }
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult CommentDelete(int id)
        {
            var user = new UserManager().GetUser((int)UserID);
            using (var commentManager = new UserCommentManager())
            {
                var list = commentManager.GetUserComments(user.ID);

                //delete comment if owned by this user
                if (list.Where(c => c.User.ID == user.ID && c.ID == id).Any())
                {
                    commentManager.DeleteComment(user.ID, id);
                }
            }

            return RedirectToAction("Comments");
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult Media()
        {
            UserManager userManager = new UserManager();

            var user = userManager.GetUser((int)UserID);
            var list = new MediaItemManager().GetUserMediaItems(user.ID).OrderByDescending(m => m.DateCreated);
            return View(list);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult MediaDelete(int id)
        {
            var user = new UserManager().GetUser((int)UserID);
            var itemManager = new MediaItemManager();
            var list = itemManager.GetUserMediaItems(user.ID);

            //delete item if owned by this user
            if (list.Where(c => c.User.ID == user.ID && c.ID == id).Any())
            {
                itemManager.DeleteMediaItem(user.ID, id);
            }

            return RedirectToAction("Media");
        }

        [Authorize(Roles = "StandardUser")]
        public async Task<ActionResult> Applications()
        {
            UserManager userManager = new UserManager();

            var user = userManager.GetUser((int)UserID);
            ViewBag.UserProfile = user;

            using (var apps = new RegisteredApplicationManager())
            {
                var summary = new ApplicationSummary();
                summary.RegisteredApplications = (await apps.Search(null, null, 1, 500, user.ID)).ToList();
                summary.AuthorizedApplications = apps.GetUserAuthorizedApplications(user.ID);
                return View(summary);
            }

        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult AppEdit(int? id)
        {
            var app = new RegisteredApplication();
            var userId = (int)UserID;

            if (id != null)
            {
                using (var appManager = new RegisteredApplicationManager())
                {
                    app = appManager.GetRegisteredApplication((int)id, userId);
                }

            }
            else
            {
                app.UserID = userId;
                app.IsEnabled = true;
                app.IsWriteEnabled = true;

            }

            return View("AppEdit", app);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult AppGenerateNewKey(int? id)
        {
            var userId = (int)UserID;

            if (id != null)
            {
                using (var appManager = new RegisteredApplicationManager())
                {
                    var app = appManager.GetRegisteredApplication((int)id, userId);

                    if (app != null)
                    {
                        app = appManager.GenerateNewAPIKey((int)id, userId);
                    }
                }

            }

            return RedirectToAction("Applications");
        }


        [Authorize(Roles = "StandardUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AppEdit(RegisteredApplication app)
        {
            if (ModelState.IsValid)
            {

                using (var appManager = new RegisteredApplicationManager())
                {
                    app = appManager.UpdateRegisteredApplication(app, UserID);
                }
                return RedirectToAction("Applications", "Profile");
            }

            return View(app);
        }

        #region OCPI Feed Submission

        /// <summary>
        /// Step 1: Show the OCPI feed submission form
        /// </summary>
        [Authorize(Roles = "StandardUser")]
        public ActionResult SubmitOCPI()
        {
            PopulateCountryList();

            // Restore form data if returning from a failed validation
            if (TempData["OCPISubmitDetails"] != null)
            {
                var model = JsonConvert.DeserializeObject<OCPISubmitModel>(TempData["OCPISubmitDetails"].ToString());
                return View(model);
            }

            return View(new OCPISubmitModel { CountryID = 2 });
        }

        /// <summary>
        /// Step 2: Validate the submitted OCPI feed and show results with operator mapping
        /// </summary>
        [Authorize(Roles = "StandardUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ValidateOCPI(OCPISubmitModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateCountryList();
                return View("SubmitOCPI", model);
            }

            var validator = new OCPIFeedValidator();
            var validationResult = await validator.ValidateFeedAsync(
                model.LocationsEndpointUrl,
                model.AuthorizationHeaderKey,
                model.AuthorizationHeaderValue
            );

            // Apply discovered endpoint and auth settings to the model so later steps store the working configuration.
            if (!string.IsNullOrEmpty(validationResult.ResolvedLocationsEndpointUrl))
            {
                model.LocationsEndpointUrl = validationResult.ResolvedLocationsEndpointUrl;
            }

            if (!string.IsNullOrEmpty(validationResult.ResolvedAuthHeaderKey))
            {
                model.AuthorizationHeaderKey = validationResult.ResolvedAuthHeaderKey;
            }

            model.AuthorizationHeaderValuePrefix = validationResult.ResolvedAuthHeaderValuePrefix;

            // Fetch available operators for the mapping dropdown
            var operators = new OperatorInfoManager().GetOperators();

            // Try to auto-match discovered operators to existing OCM operators
            foreach (var discovered in validationResult.DiscoveredOperators)
            {
                var match = operators.FirstOrDefault(o =>
                    o.Title != null &&
                    (o.Title.Equals(discovered.Name, StringComparison.OrdinalIgnoreCase)
                    || o.Title.Replace(" ", "").Equals(discovered.Name.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)));

                if (match != null)
                {
                    discovered.MappedOperatorId = match.ID;
                }
                else
                {
                    discovered.IsNewOperator = true;
                }
            }

            var validateModel = new OCPIValidateModel
            {
                SubmitDetails = model,
                ValidationResult = validationResult,
                AvailableOperators = operators,
                OperatorMappings = validationResult.DiscoveredOperators
                    .ToDictionary(o => o.Name, o => o.MappedOperatorId ?? 0)
            };

            // Store submit details in TempData for the next step
            TempData["OCPISubmitDetails"] = JsonConvert.SerializeObject(model);
            TempData["OCPIValidationResult"] = JsonConvert.SerializeObject(validationResult);

            return View(validateModel);
        }

        /// <summary>
        /// Step 3: Show the confirmation page with data sharing agreement
        /// </summary>
        [Authorize(Roles = "StandardUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmOCPI(OCPIValidateModel model)
        {
            // Recover submit details from TempData
            OCPISubmitModel submitDetails;
            OCPIValidationResult validationResult;

            if (TempData["OCPISubmitDetails"] != null)
            {
                submitDetails = JsonConvert.DeserializeObject<OCPISubmitModel>(TempData["OCPISubmitDetails"].ToString());
                validationResult = JsonConvert.DeserializeObject<OCPIValidationResult>(TempData["OCPIValidationResult"].ToString());
            }
            else
            {
                return RedirectToAction("SubmitOCPI");
            }

            var confirmModel = new OCPIConfirmModel
            {
                SubmitDetails = submitDetails,
                ValidationResult = validationResult,
                OperatorMappings = model.OperatorMappings ?? new Dictionary<string, int>(),
                DefaultOperatorId = model.DefaultOperatorId
            };

            // Persist for the final submit
            TempData["OCPISubmitDetails"] = JsonConvert.SerializeObject(submitDetails);
            TempData["OCPIValidationResult"] = JsonConvert.SerializeObject(validationResult);
            TempData["OCPIOperatorMappings"] = JsonConvert.SerializeObject(confirmModel.OperatorMappings);
            TempData["OCPIDefaultOperatorId"] = confirmModel.DefaultOperatorId;

            return View(confirmModel);
        }

        /// <summary>
        /// Final step: create the DataProvider and OCPI config
        /// </summary>
        [Authorize(Roles = "StandardUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CompleteOCPI(OCPIConfirmModel model)
        {
            if (!model.AcceptedDataSharingAgreement)
            {
                ModelState.AddModelError(nameof(model.AcceptedDataSharingAgreement), "You must accept the data sharing agreement.");
            }

            // Recover state from TempData
            OCPISubmitModel submitDetails;
            OCPIValidationResult validationResult;
            Dictionary<string, int> operatorMappings;
            int? defaultOperatorId;

            try
            {
                submitDetails = JsonConvert.DeserializeObject<OCPISubmitModel>(TempData["OCPISubmitDetails"]?.ToString());
                validationResult = JsonConvert.DeserializeObject<OCPIValidationResult>(TempData["OCPIValidationResult"]?.ToString());
                operatorMappings = JsonConvert.DeserializeObject<Dictionary<string, int>>(TempData["OCPIOperatorMappings"]?.ToString());
                defaultOperatorId = TempData["OCPIDefaultOperatorId"] as int?;
            }
            catch
            {
                return RedirectToAction("SubmitOCPI");
            }

            if (!ModelState.IsValid)
            {
                var confirmModel = new OCPIConfirmModel
                {
                    SubmitDetails = submitDetails,
                    ValidationResult = validationResult,
                    OperatorMappings = operatorMappings,
                    DefaultOperatorId = defaultOperatorId
                };
                // Re-persist for retry
                TempData["OCPISubmitDetails"] = JsonConvert.SerializeObject(submitDetails);
                TempData["OCPIValidationResult"] = JsonConvert.SerializeObject(validationResult);
                TempData["OCPIOperatorMappings"] = JsonConvert.SerializeObject(operatorMappings);
                TempData["OCPIDefaultOperatorId"] = defaultOperatorId;
                return View("ConfirmOCPI", confirmModel);
            }

            // Build OCPI provider config JSON
            var providerConfig = new
            {
                ProviderName = submitDetails.ProviderName.ToLower().Replace(" ", "-"),
                OutputNamePrefix = submitDetails.ProviderName.ToLower().Replace(" ", "-"),
                Description = $"User-submitted OCPI feed: {submitDetails.ProviderName}",
                DataProviderId = 0, // will be updated after DataProvider is created
                LocationsEndpointUrl = submitDetails.LocationsEndpointUrl,
                AuthHeaderKey = submitDetails.AuthorizationHeaderKey,
                AuthHeaderValuePrefix = submitDetails.AuthorizationHeaderValuePrefix,
                CredentialKey = !string.IsNullOrEmpty(submitDetails.AuthorizationHeaderValue)
                    ? $"OCPI-{submitDetails.ProviderName.ToUpper().Replace(" ", "-")}"
                    : (string)null,
                DefaultOperatorId = defaultOperatorId,
                IsAutoRefreshed = true,
                IsProductionReady = false, // pending admin approval
                IsEnabled = false, // pending admin approval
                AllowDuplicatePOIWithDifferentOperator = true,
                OperatorMappings = operatorMappings?.Where(m => m.Value > 0).ToDictionary(m => m.Key, m => m.Value) ?? new Dictionary<string, int>(),
                ExcludedLocationIds = new List<string>(),
                // Track new operators that need to be created
                NewOperatorsRequired = operatorMappings?.Where(m => m.Value == 0).Select(m => m.Key).ToList() ?? new List<string>(),
                SubmittedByUserId = (int)UserID,
                SubmittedDate = DateTime.UtcNow,
                ValidationSummary = new
                {
                    validationResult.LocationCount,
                    validationResult.EvseCount,
                    validationResult.DiscoveredCountries
                }
            };

            var configJson = JsonConvert.SerializeObject(providerConfig, Formatting.Indented);

            // Create the DataProvider
            try
            {
                using var agreementManager = new DataSharingAgreementManager();
                var agreement = agreementManager.CreateAgreement(new DataSharingAgreement
                {
                    CompanyName = submitDetails.CompanyName,
                    CountryID = submitDetails.CountryID,
                    RepresentativeName = submitDetails.RepresentativeName,
                    ContactEmail = submitDetails.ContactEmail,
                    WebsiteURL = submitDetails.WebsiteUrl,
                    DataFeedType = submitDetails.DataFeedType,
                    DataFeedURL = submitDetails.LocationsEndpointUrl,
                    DataLicense = "CC-0",
                    Credentials = submitDetails.AuthorizationHeaderValue,
                    Comments = $"OCPI feed submission for provider '{submitDetails.ProviderName}'"
                }, (int)UserID);

                using var dpManager = new DataProviderManager();
                var dataProvider = dpManager.CreateOCPIDataProvider(
                    title: submitDetails.ProviderName,
                    websiteUrl: submitDetails.WebsiteUrl,
                    license: "Licensed under CC0 by data sharing agreement",
                    isOpenDataLicensed: true,
                    ocpiConfigJson: configJson,
                    submittedByUserId: (int)UserID,
                    dataSharingAgreementId: agreement.ID
                );

                return View("OCPISubmissionResult", new OCPISubmissionResultModel
                {
                    IsSuccess = true,
                    DataProviderId = dataProvider.ID,
                    ProviderName = submitDetails.ProviderName,
                    Message = "Your OCPI feed has been submitted successfully and is pending admin review."
                });
            }
            catch (Exception ex)
            {
                return View("OCPISubmissionResult", new OCPISubmissionResultModel
                {
                    IsSuccess = false,
                    ProviderName = submitDetails.ProviderName,
                    Message = $"An error occurred while creating your submission: {ex.Message}"
                });
            }
        }

        private void PopulateCountryList()
        {
            using (var refDataManager = new ReferenceDataManager())
            {
                ViewBag.CountryList = new SelectList(refDataManager.GetCountries(false), "ID", "Title");
            }
        }

        #endregion
    }
}