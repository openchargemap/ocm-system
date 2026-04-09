using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.API.Utils;
using OCM.Core.Data;
using OCM.Import.Providers.OCPI;
using OCM.Web.Models;
using OCM.Web.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgreementModel = OCM.API.Common.Model.DataSharingAgreement;
using ModelDataProviderStatusType = OCM.API.Common.Model.DataProviderStatusType;
using ProviderModel = OCM.API.Common.Model.DataProvider;

namespace OCM.MVC.Controllers
{
    public class AdminController : BaseController
    {
        private IHostEnvironment _host;
        private IMemoryCache _cache;
        private IAdminTaskService _adminTaskService;
        private IImportQueueService _importQueueService;

        public AdminController(IHostEnvironment host, IMemoryCache memoryCache, IAdminTaskService adminTaskService, IImportQueueService importQueueService)
        {
            _host = host;
            _cache = memoryCache;
            _adminTaskService = adminTaskService;
            _importQueueService = importQueueService;
        }

        private void PopulateCountryList(int selectedCountryId)
        {
            var countryList = new ReferenceDataManager().GetCountries(false);
            ViewBag.CountryList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(countryList, "ID", "Title", selectedCountryId);
        }

        private static List<ModelDataProviderStatusType> GetDataProviderStatusTypes()
        {
            using var dataProviderManager = new DataProviderManager();
            return dataProviderManager.GetDataProviderStatusTypes();
        }

        private static OCPIProviderConfiguration GetStoredProviderConfiguration(string importConfig)
        {
            if (string.IsNullOrWhiteSpace(importConfig))
            {
                return null;
            }

            try
            {
                var token = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JToken>(importConfig);
                if (token == null)
                {
                    return null;
                }

                if (token.Type == Newtonsoft.Json.Linq.JTokenType.Object && token["Providers"] is Newtonsoft.Json.Linq.JArray providersArray)
                {
                    return providersArray.FirstOrDefault()?.ToObject<OCPIProviderConfiguration>();
                }

                if (token.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    return token.FirstOrDefault()?.ToObject<OCPIProviderConfiguration>();
                }

                return token.ToObject<OCPIProviderConfiguration>();
            }
            catch
            {
                return null;
            }
        }

        private static string FormatOperatorMappings(Dictionary<string, int> operatorMappings)
        {
            if (operatorMappings == null || operatorMappings.Count == 0)
            {
                return null;
            }

            return string.Join(Environment.NewLine, operatorMappings
                .OrderBy(m => m.Key)
                .Select(m => $"{m.Key}={m.Value}"));
        }

        private static string FormatExcludedLocationIds(List<string> excludedLocationIds)
        {
            if (excludedLocationIds == null || excludedLocationIds.Count == 0)
            {
                return null;
            }

            return string.Join(Environment.NewLine, excludedLocationIds.OrderBy(i => i));
        }

        private AdminDataSharingAgreementEditModel BuildReviewEditModel(AgreementModel agreement, ProviderModel dataProvider, string importConfig, OCPIValidationResult validationPreview)
        {
            var existingConfig = GetStoredProviderConfiguration(importConfig);
            var providerName = existingConfig?.ProviderName;

            if (string.IsNullOrWhiteSpace(providerName))
            {
                providerName = ((agreement.CompanyName ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "-").Replace("_", "-"));
            }

            return new AdminDataSharingAgreementEditModel
            {
                AgreementId = agreement.ID,
                DataProviderId = dataProvider?.ID,
                CompanyName = agreement.CompanyName,
                CountryId = agreement.CountryID,
                RepresentativeName = agreement.RepresentativeName,
                ContactEmail = agreement.ContactEmail,
                WebsiteUrl = agreement.WebsiteURL,
                DataFeedType = agreement.DataFeedType,
                SubmittedFeedUrl = agreement.DataFeedURL,
                SubmittedCredentials = agreement.Credentials,
                AgreementComments = agreement.Comments,
                ProviderName = providerName,
                DataProviderStatusTypeId = dataProvider?.DataProviderStatusType?.ID,
                OutputNamePrefix = existingConfig?.OutputNamePrefix,
                DataProviderOcpiId = dataProvider?.ID ?? 0,
                LocationsEndpointUrl = existingConfig?.LocationsEndpointUrl ?? validationPreview?.ResolvedLocationsEndpointUrl ?? agreement.DataFeedURL,
                AuthHeaderKey = existingConfig?.AuthHeaderKey ?? validationPreview?.ResolvedAuthHeaderKey,
                AuthHeaderValuePrefix = existingConfig?.AuthHeaderValuePrefix ?? validationPreview?.ResolvedAuthHeaderValuePrefix ?? "Token ",
                CredentialKey = existingConfig?.CredentialKey,
                DefaultOperatorId = existingConfig?.DefaultOperatorId,
                IsEnabled = existingConfig?.IsEnabled ?? true,
                IsAutoRefreshed = existingConfig?.IsAutoRefreshed ?? true,
                IsProductionReady = existingConfig?.IsProductionReady ?? false,
                AllowDuplicatePOIWithDifferentOperator = existingConfig?.AllowDuplicatePOIWithDifferentOperator ?? true,
                Description = existingConfig?.Description,
                OperatorMappingsText = FormatOperatorMappings(existingConfig?.OperatorMappings),
                ExcludedLocationIdsText = FormatExcludedLocationIds(existingConfig?.ExcludedLocationIds),
                ApproveImport = dataProvider?.IsApprovedImport == true
            };
        }

        private AdminDataSharingAgreementReviewModel BuildReviewModel(AgreementModel agreement, ProviderModel dataProvider, string importConfig, OCPIValidationResult validationPreview, AdminDataSharingAgreementEditModel review = null, ImportJobViewModel currentImportJob = null)
        {
            return new AdminDataSharingAgreementReviewModel
            {
                Agreement = agreement,
                DataProvider = dataProvider,
                ImportConfig = importConfig,
                ValidationPreview = validationPreview,
                AvailableDataProviderStatuses = GetDataProviderStatusTypes(),
                AvailableOperators = new OperatorInfoManager().GetOperators(),
                CurrentImportJob = currentImportJob ?? _importQueueService.GetLatestJobForAgreement(agreement.ID),
                Review = review ?? BuildReviewEditModel(agreement, dataProvider, importConfig, validationPreview)
            };
        }

        private async Task WriteServerSentEventAsync(string eventType, string data, CancellationToken cancellationToken)
        {
            var payload = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                payload.Append("event: ").Append(eventType).Append('\n');
            }

            var lines = (data ?? string.Empty)
                .Replace("\r", string.Empty)
                .Split('\n');

            foreach (var line in lines)
            {
                payload.Append("data: ").Append(line).Append('\n');
            }

            payload.Append('\n');

            await Response.WriteAsync(payload.ToString(), cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        private static Dictionary<string, int> ParseOperatorMappings(string operatorMappingsText)
        {
            var mappings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(operatorMappingsText))
            {
                return mappings;
            }

            var lines = operatorMappingsText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l));

            foreach (var line in lines)
            {
                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, separatorIndex).Trim();
                var valueText = line.Substring(separatorIndex + 1).Trim();
                if (!string.IsNullOrWhiteSpace(key) && int.TryParse(valueText, out var value) && value > 0)
                {
                    mappings[key] = value;
                }
            }

            return mappings;
        }

        private static List<string> ParseExcludedLocationIds(string excludedLocationIdsText)
        {
            if (string.IsNullOrWhiteSpace(excludedLocationIdsText))
            {
                return new List<string>();
            }

            return excludedLocationIdsText
                .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static OCPIProviderConfiguration BuildProviderConfiguration(AdminDataSharingAgreementEditModel review, int dataProviderId)
        {
            return new OCPIProviderConfiguration
            {
                ProviderName = review.ProviderName?.Trim(),
                OutputNamePrefix = string.IsNullOrWhiteSpace(review.OutputNamePrefix) ? review.ProviderName?.Trim() : review.OutputNamePrefix.Trim(),
                Description = review.Description,
                DataProviderId = dataProviderId,
                LocationsEndpointUrl = review.LocationsEndpointUrl?.Trim(),
                AuthHeaderKey = string.IsNullOrWhiteSpace(review.AuthHeaderKey) ? null : review.AuthHeaderKey.Trim(),
                CredentialKey = string.IsNullOrWhiteSpace(review.CredentialKey) ? null : review.CredentialKey.Trim(),
                AuthHeaderValuePrefix = review.AuthHeaderValuePrefix == null ? null : review.AuthHeaderValuePrefix.Trim(),
                DefaultOperatorId = review.DefaultOperatorId,
                IsAutoRefreshed = review.IsAutoRefreshed,
                IsProductionReady = review.IsProductionReady,
                AllowDuplicatePOIWithDifferentOperator = review.AllowDuplicatePOIWithDifferentOperator,
                OperatorMappings = ParseOperatorMappings(review.OperatorMappingsText),
                ExcludedLocationIds = ParseExcludedLocationIds(review.ExcludedLocationIdsText),
                IsEnabled = review.IsEnabled
            };
        }

        private async Task<OCPIValidationResult> BuildValidationPreviewAsync(AdminDataSharingAgreementEditModel review)
        {
            var validator = new OCPIFeedValidator();
            var authHeaderKey = string.IsNullOrWhiteSpace(review.AuthHeaderKey) ? null : review.AuthHeaderKey;
            var submittedCredentials = string.IsNullOrWhiteSpace(review.SubmittedCredentials) ? null : review.SubmittedCredentials;
            return await validator.ValidateFeedAsync(review.LocationsEndpointUrl, authHeaderKey, submittedCredentials);
        }

        // GET: /Admin/
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            return View();
        }



        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users(string sortOrder, string keyword, int pageIndex = 1, int pageSize = 50)
        {

            ViewData["keyword"] = keyword;
            ViewData["sortorder"] = sortOrder;

            PaginatedCollection<API.Common.Model.User> userList = await new UserManager().GetUsers(sortOrder, keyword, pageIndex, pageSize);
            return View(userList);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult EditUser(int id)
        {
            var user = new UserManager().GetUser(id);
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditUser(OCM.API.Common.Model.User userDetails)
        {
            if (ModelState.IsValid)
            {
                var userManager = new UserManager();

                //save
                if (userManager.UpdateUserProfile(userDetails, true))
                {
                    return RedirectToAction("Users");
                }
            }

            return View(userDetails);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult PromoteUserToEditor(int userId, int countryId, bool autoCreateSubscriptions, bool removePermission)
        {
            new UserManager().PromoteUserToCountryEditor((int)HttpContext.Session.GetInt32("UserID"), userId, countryId, autoCreateSubscriptions, removePermission);
            return RedirectToAction("View", "Profile", new { id = userId });
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ConvertPermissions()
        {
            //convert all systemUser permission to new format where applicable
            new UserManager().ConvertUserPermissions();
            return RedirectToAction("Index", "Admin", new { result = "processed" });
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Operators()
        {
            var operatorInfoManager = new OperatorInfoManager();

            return View(operatorInfoManager.GetOperators());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult DataProviders(string sortBy = "title")
        {
            var providers = new ReferenceDataManager().GetDataProviders();

            providers = string.Equals(sortBy, "lastimported", StringComparison.OrdinalIgnoreCase)
                ? providers.OrderByDescending(p => p.DateLastImported ?? DateTime.MinValue).ThenBy(p => p.Title).ToList()
                : providers.OrderBy(p => p.Title).ToList();

            ViewBag.SortBy = sortBy;

            return View(providers);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult DataSharingAgreements()
        {
            using var agreementManager = new DataSharingAgreementManager();
            using var dataProviderManager = new DataProviderManager();

            var model = agreementManager.GetAgreements()
                .Select(a => new AdminDataSharingAgreementListItem
                {
                    Agreement = a,
                    DataProvider = dataProviderManager.GetDataProviderByAgreementId(a.ID),
                    CurrentImportJob = _importQueueService.GetLatestJobForAgreement(a.ID)
                })
                .ToList();

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ReviewDataSharingAgreement(int id, Guid? jobId = null)
        {
            using var agreementManager = new DataSharingAgreementManager();
            using var dataProviderManager = new DataProviderManager();

            var agreement = agreementManager.GetAgreement(id);
            if (agreement == null)
            {
                return RedirectToAction("DataSharingAgreements");
            }

            var dataProvider = dataProviderManager.GetDataProviderByAgreementId(id);
            var importConfig = dataProviderManager.GetImportConfigByAgreementId(id);

            var existingConfig = GetStoredProviderConfiguration(importConfig);
            var validationPreview = await new OCPIFeedValidator().ValidateFeedAsync(
                existingConfig?.LocationsEndpointUrl ?? agreement.DataFeedURL,
                existingConfig?.AuthHeaderKey,
                agreement.Credentials);

            PopulateCountryList(agreement.CountryID);

            var currentImportJob = jobId.HasValue
                ? _importQueueService.GetJob(jobId.Value)
                : _importQueueService.GetLatestJobForAgreement(agreement.ID);

            if (currentImportJob != null && currentImportJob.AgreementId != agreement.ID)
            {
                currentImportJob = _importQueueService.GetLatestJobForAgreement(agreement.ID);
            }

            var model = BuildReviewModel(agreement, dataProvider, importConfig, validationPreview, currentImportJob: currentImportJob);

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReviewDataSharingAgreement([Bind(Prefix = "Review")] AdminDataSharingAgreementEditModel review)
        {
            review ??= new AdminDataSharingAgreementEditModel();

            using var agreementManager = new DataSharingAgreementManager();
            using var dataProviderManager = new DataProviderManager();

            var agreement = agreementManager.GetAgreement(review.AgreementId);
            if (agreement == null)
            {
                return RedirectToAction("DataSharingAgreements");
            }

            var dataProvider = dataProviderManager.GetDataProviderByAgreementId(review.AgreementId);

            PopulateCountryList(review.CountryId);

            OCPIValidationResult validationPreview = null;
            if (ModelState.IsValid)
            {
                validationPreview = await BuildValidationPreviewAsync(review);
            }

            if (!string.IsNullOrWhiteSpace(review.AuthHeaderKey)
                && string.IsNullOrWhiteSpace(review.CredentialKey)
                && string.IsNullOrWhiteSpace(review.SubmittedCredentials))
            {
                ModelState.AddModelError(nameof(review.CredentialKey), "Provide a key vault secret name or keep the submitted credentials when the feed requires an authorization header.");
            }

            if (review.DefaultOperatorId.HasValue && review.DefaultOperatorId <= 0)
            {
                ModelState.AddModelError(nameof(review.DefaultOperatorId), "Default operator must be a valid operator ID.");
            }

            if (review.DataProviderStatusTypeId.HasValue && review.DataProviderStatusTypeId <= 0)
            {
                ModelState.AddModelError(nameof(review.DataProviderStatusTypeId), "Data provider status must be a valid status type.");
            }

            if (review.DataProviderId.HasValue && review.DataProviderOcpiId.HasValue && review.DataProviderOcpiId.Value > 0 && review.DataProviderId.Value != review.DataProviderOcpiId.Value)
            {
                ModelState.AddModelError(nameof(review.DataProviderOcpiId), "Configured OCPI data provider ID must match the linked data provider.");
            }

            if (ModelState.IsValid)
            {
                agreement.CompanyName = review.CompanyName;
                agreement.CountryID = review.CountryId;
                agreement.RepresentativeName = review.RepresentativeName;
                agreement.ContactEmail = review.ContactEmail;
                agreement.WebsiteURL = review.WebsiteUrl;
                agreement.DataFeedType = review.DataFeedType;
                agreement.DataFeedURL = review.SubmittedFeedUrl;
                agreement.Credentials = review.SubmittedCredentials;
                agreement.Comments = review.AgreementComments;

                agreement = agreementManager.UpdateAgreement(agreement);

                if (dataProvider == null)
                {
                    dataProvider = dataProviderManager.CreateOCPIDataProvider(
                        title: review.CompanyName,
                        websiteUrl: review.WebsiteUrl,
                        license: agreement.DataLicense,
                        isOpenDataLicensed: true,
                        ocpiConfigJson: string.Empty,
                        submittedByUserId: (int)UserID,
                        dataSharingAgreementId: review.AgreementId);
                }

                review.DataProviderId = dataProvider.ID;
                review.DataProviderOcpiId = dataProvider.ID;

                var providerConfig = BuildProviderConfiguration(review, dataProvider.ID);
                var importConfig = JsonConvert.SerializeObject(providerConfig, Formatting.Indented);

                dataProvider = dataProviderManager.UpdateOCPIDataProvider(
                    dataProviderId: dataProvider.ID,
                    title: review.CompanyName,
                    websiteUrl: review.WebsiteUrl,
                    license: agreement.DataLicense,
                    isOpenDataLicensed: true,
                    ocpiConfigJson: importConfig,
                    dataProviderStatusTypeId: review.DataProviderStatusTypeId,
                    updatedByUserId: (int)UserID);

                dataProviderManager.SetImportApprovalStatus(dataProvider.ID, review.ApproveImport);

                TempData["StatusMessage"] = "OCPI review details saved.";

                return RedirectToAction(nameof(ReviewDataSharingAgreement), new { id = review.AgreementId });
            }

            var importConfigForDisplay = dataProviderManager.GetImportConfigByAgreementId(review.AgreementId);
            var viewModel = BuildReviewModel(agreement, dataProvider, importConfigForDisplay, validationPreview ?? new OCPIValidationResult(), review);
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveDataSharingAgreement(int id)
        {
            using var dataProviderManager = new DataProviderManager();
            var dataProvider = dataProviderManager.GetDataProviderByAgreementId(id);
            if (dataProvider != null)
            {
                dataProviderManager.SetImportApprovalStatus(dataProvider.ID, true);
            }

            return RedirectToAction("ReviewDataSharingAgreement", new { id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteDataSharingAgreement(int id)
        {
            using var agreementManager = new DataSharingAgreementManager();

            if (agreementManager.DeleteAgreement(id, (int)UserID))
            {
                TempData["StatusMessage"] = $"Data sharing agreement #{id} deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Data sharing agreement #{id} was not found.";
            }

            return RedirectToAction(nameof(DataSharingAgreements));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QueueDataSharingAgreementImport(int id)
        {
            return QueueDataSharingAgreementJob(id, ImportJobMode.Import);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QueueApprovedDataSharingAgreementImports()
        {
            var jobs = _importQueueService.QueueApprovedImports((int)UserID, ImportJobMode.Import);
            TempData["StatusMessage"] = jobs.Count == 1
                ? "Queued 1 approved import."
                : $"Queued {jobs.Count} approved imports.";

            return RedirectToAction(nameof(DataSharingAgreements));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PreviewDataSharingAgreementImport(int id)
        {
            return QueueDataSharingAgreementJob(id, ImportJobMode.Preview);
        }

        private ActionResult QueueDataSharingAgreementJob(int id, ImportJobMode mode)
        {
            using var agreementManager = new DataSharingAgreementManager();
            using var dataProviderManager = new DataProviderManager();

            var agreement = agreementManager.GetAgreement(id);
            if (agreement == null)
            {
                return RedirectToAction(nameof(DataSharingAgreements));
            }

            var importConfig = dataProviderManager.GetImportConfigByAgreementId(id);
            if (string.IsNullOrWhiteSpace(importConfig))
            {
                TempData["ErrorMessage"] = "Save a stored OCPI import configuration before queueing an import.";
                return RedirectToAction(nameof(ReviewDataSharingAgreement), new { id });
            }

            var importJob = _importQueueService.QueueImport(id, (int)UserID, mode);
            TempData["StatusMessage"] = $"Queued {mode.ToString().ToLowerInvariant()} job {importJob.JobId} for agreement #{id}.";

            return RedirectToAction(nameof(ReviewDataSharingAgreement), new { id });
        }

        [Authorize(Roles = "Admin")]
        public async Task StreamDataSharingAgreementImportLogs(Guid jobId, CancellationToken cancellationToken)
        {
            var importJob = _importQueueService.GetJob(jobId);
            if (importJob == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            await foreach (var message in _importQueueService.StreamJobEventsAsync(jobId, cancellationToken))
            {
                await WriteServerSentEventAsync(message.EventType, JsonConvert.SerializeObject(message), cancellationToken);
            }
        }

        [Authorize(Roles = "Admin")]
        public ActionResult EditOperator(int? id)
        {
            var operatorInfo = new OperatorInfo();

            if (id != null) operatorInfo = new OperatorInfoManager().GetOperatorInfo((int)id);
            return View(operatorInfo);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditOperator(OperatorInfo operatorInfo)
        {
            if (ModelState.IsValid)
            {
                var operatorInfoManager = new OperatorInfoManager();

                operatorInfo = operatorInfoManager.UpdateOperatorInfo((int)UserID, operatorInfo);

                return RedirectToAction("Operators", "Admin");
            }
            return View(operatorInfo);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisteredApplications(string sortOrder, string keyword, int pageIndex = 1, int pageSize = 50)
        {

            ViewData["keyword"] = keyword;
            ViewData["sortorder"] = sortOrder;
            using (var appManager = new RegisteredApplicationManager())
            {
                PaginatedCollection<API.Common.Model.RegisteredApplication> list = await appManager.Search(sortOrder, keyword, pageIndex, pageSize);
                return View(list);
            }
        }

        [Authorize(Roles = "Admin")]
        public ActionResult AppEdit(int? id)
        {
            var app = new API.Common.Model.RegisteredApplication();
            var userId = (int)UserID;
            ViewBag.IsAdmin = true;

            if (id != null)
            {
                using (var appManager = new RegisteredApplicationManager())
                {
                    app = appManager.GetRegisteredApplication((int)id, null);
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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AppEdit(API.Common.Model.RegisteredApplication app)
        {
            ViewBag.IsAdmin = true;
            if (ModelState.IsValid)
            {

                app = new RegisteredApplicationManager().UpdateRegisteredApplication(app, null);
                return RedirectToAction("RegisteredApplications", "Admin");
            }

            return View(app);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult CommentDelete(int id)
        {
            var commentManager = new UserCommentManager();
            var user = new UserManager().GetUser((int)UserID);
            commentManager.DeleteComment(user.ID, id);
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult MediaDelete(int id)
        {
            var itemManager = new MediaItemManager();
            var user = new UserManager().GetUser((int)UserID);
            itemManager.DeleteMediaItem(user.ID, id);
            return RedirectToAction("Details", "POI");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult MediaExportTest()
        {
            var files = System.IO.File.ReadAllLines(@"d:\temp\ocm-images\export.txt");

            var itemManager = new MediaItemManager();

            //for each file, download original, medium and thumnail, then upload s3
            foreach (var f in files)
            {
                var fields = f.Split('\t');

                var sourceURL = fields[1];

                System.Diagnostics.Debug.WriteLine(sourceURL);
                using (var client = new System.Net.WebClient())
                {
                    //https://ocm.blob.core.windows.net/images/IT/OCM85012/OCM-85012.orig.2017043019024667.jpg
                    var filename = sourceURL.Replace("https://ocm.blob.core.windows.net/images/", "");

                    var output = System.IO.Path.Combine(@"d:\temp\ocm-images-export\", filename);

                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
                    client.DownloadFile(sourceURL, output);

                    var medURL = sourceURL.Replace(".orig.", ".medi.");
                    client.DownloadFile(medURL, output.Replace(".orig.", ".medi."));

                    var thmbURL = sourceURL.Replace(".orig.", ".thmb.");
                    client.DownloadFile(medURL, output.Replace(".orig.", ".thmb."));
                }
            }
            return RedirectToAction("Index");
        }

        public async Task<JsonResult> PollForTasks(string key)
        {
            var notificationsSent = 0;
            var itemsAutoApproved = 0;
            var exceptionCount = 0;
            var logItems = new List<string>();

            MirrorStatus mirrorStatus = null;

            //poll for periodic tasks (subscription notifications etc)
            if (key == System.Configuration.ConfigurationManager.AppSettings["AdminPollingAPIKey"])
            {
                //send all pending subscription notifications
                if (bool.Parse(ConfigurationManager.AppSettings["EnableSubscriptionChecks"]) == true)
                {
                    try
                    {
                        //TODO: can't run in seperate async thread becuase HttpContext is not available
                        string templateFolderPath = _host.ContentRootPath + "/templates/Notifications";

                        notificationsSent = await new UserSubscriptionManager().SendAllPendingSubscriptionNotifications(templateFolderPath);

                    }
                    catch (Exception)
                    {
                        ; ; //failed to send notifications
                    }
                }


                var autoApproveDays = 3;

                var poiManager = new POIManager();

                var userManager = new UserManager();
                var systemUser = userManager.GetUser((int)StandardUsers.System);

                // check for edit queue items to auto approve 
                using (var editQueueManager = new EditQueueManager())
                {
                    var queueItems = (await editQueueManager.GetEditQueueItems(new EditQueueFilter { ShowProcessed = false, ShowEditsOnly = false }))
                        .Where(q => q.DateProcessed == null).ToList()
                        .OrderBy(q => q.DateSubmitted);

                    foreach (var i in queueItems)
                    {
                        try
                        {
                            var editPOI = JsonConvert.DeserializeObject<OCM.API.Common.Model.ChargePoint>(i.EditData);
                            var submitter = userManager.GetUser(i.User.ID);

                            var submitterHasEditPermissions = UserManager.HasUserPermission(submitter, editPOI.AddressInfo.CountryID, PermissionLevel.Editor);

                            // auto approve if submitter has edit permissions, or edit has been in queue for more than 3 days
                            if (submitterHasEditPermissions || i.DateSubmitted < DateTime.UtcNow.AddDays(-autoApproveDays))
                            {

                                await editQueueManager.ProcessEditQueueItem(i.ID, true, (int)StandardUsers.System, false, "Auto Approved");

                                itemsAutoApproved++;
                            }
                            else
                            {
                                if (submitter.ReputationPoints >= 50 && submitter.Permissions == null)
                                {
                                    logItems.Add($"User {submitter.Username} [{submitter.ID}] has high reputation [{submitter.ReputationPoints}] but no country editor permissions.");
                                }
                            }
                        }
                        catch
                        {
                            exceptionCount++;
                        }
                    }
                }


                // auto approve new items if they have been in the system for more than 3 days
                var newPois = await poiManager.GetPOIListAsync(new APIRequestParams { SubmissionStatusTypeID = new int[] { 1 } });

                foreach (var poi in newPois)
                {
                    if (poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Submitted_UnderReview || poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Imported_UnderReview)
                    {

                        if (poi.DateCreated < DateTime.UtcNow.AddDays(-autoApproveDays))
                        {
                            try
                            {
                                poi.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Submitted_Published;
                                await new SubmissionManager().PerformPOISubmission(poi, systemUser);
                                itemsAutoApproved++;
                            }
                            catch
                            {
                                exceptionCount++;
                            }
                        }

                    }
                }

                //update cache mirror
                try
                {
                    _cache.Set("_MirrorRefreshInProgress", true);
                    mirrorStatus = await CacheManager.RefreshCachedData(CacheUpdateStrategy.Modified);
                }
                catch (Exception)
                {
                    ; ;//failed to update cache
                }
                finally
                {
                    _cache.Set("_MirrorRefreshInProgress", false);
                }

                //update stats
                if (_cache.Get("_StatsRefreshed") == null)
                {
                    using (var dataSummaryManager = new API.Common.DataSummary.DataSummaryManager())
                    {
                        await dataSummaryManager.RefreshStats();
                        _cache.Set("_StatsRefreshed", true);
                    }

                }
            }
            return Json(new
            {
                NotificationsSent = notificationsSent,
                MirrorStatus = mirrorStatus,
                ExceptionCount = exceptionCount,
                AutoApproved = itemsAutoApproved,
                Log = logItems
            });
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CheckPOIMirrorStatus(bool includeDupeCheck = false)
        {
            var status = await CacheManager.GetCacheStatus(includeDupeCheck);
            if (status == null)
            {
                status = new MirrorStatus();
                status.StatusCode = System.Net.HttpStatusCode.NotFound;
                status.Description = "Cache is offline";
            }
            else
            {
                if (_cache.TryGetValue<bool>("_MirrorRefreshInProgress", out var inProgress) && inProgress == true)
                {
                    status.Description += " (Update in progress)";
                }
            }
            return View(status);
        }

        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> RefreshPOIMirror(string mode)
        {
            MirrorStatus status = new MirrorStatus();

            if (_cache.TryGetValue<bool>("_MirrorRefreshInProgress", out var inProgress) && inProgress == true)
            {

                status.StatusCode = System.Net.HttpStatusCode.PartialContent;
                status.Description = "Update currently in progress";
            }
            else
            {
                _cache.Set("_MirrorRefreshInProgress", true);

                try
                {
                    if (mode == "repeat")
                    {
                        status = await CacheManager.RefreshCachedData(CacheUpdateStrategy.Incremental);
                        while (status.NumPOILastUpdated > 0)
                        {
                            System.Diagnostics.Debug.WriteLine("Mirror Update:" + status.LastUpdated + " updated, " + status.TotalPOIInCache + " total");
                            status = await CacheManager.RefreshCachedData(CacheUpdateStrategy.Incremental);
                        }
                    }
                    else
                        if (mode == "all")
                    {
                        status = await CacheManager.RefreshCachedData(CacheUpdateStrategy.All);
                    }
                    else
                    {
                        status = await CacheManager.RefreshCachedData(CacheUpdateStrategy.Modified);
                    }
                }
                catch (Exception exp)
                {
                    status.TotalPOIInCache = 0;
                    status.Description = "Cache update error:" + exp.ToString();
                    status.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                }

                _cache.Set("_MirrorRefreshInProgress", false);
            }


            return Json(status);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ConfigCheck()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Benchmarks()
        {
            var cache = new CacheProviderMongoDB();
            var results = await cache.PerformPOIQueryBenchmark(10, "bounding");
            return View(results);
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> MediaItemsWithoutThumbnails()
        {
            var mediaManager = new MediaItemManager();
            var items = await mediaManager.GetMediaItemsWithMissingThumbnails(100);

            ViewBag.TotalCount = items.Count;
            return View(items);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<JsonResult> ReprocessMediaItem(int id)
        {
            var mediaManager = new MediaItemManager();
            string tempFolder = Path.Combine(Path.GetTempPath(), "OCM_MediaReprocess");

            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }

            var result = await mediaManager.ReprocessMediaItem(id, tempFolder);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                thumbnailUrl = result.ThumbnailUrl,
                mediumUrl = result.MediumUrl
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<JsonResult> ReprocessAllMissingThumbnails(int batchSize = 10)
        {
            var mediaManager = new MediaItemManager();
            var items = await mediaManager.GetMediaItemsWithMissingThumbnails(batchSize);

            string tempFolder = Path.Combine(Path.GetTempPath(), "OCM_MediaReprocess");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }

            int successCount = 0;
            int failureCount = 0;
            var results = new List<object>();

            foreach (var item in items)
            {
                var result = await mediaManager.ReprocessMediaItem(item.Id, tempFolder);

                if (result.Success)
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                }

                results.Add(new
                {
                    id = item.Id,
                    success = result.Success,
                    message = result.Message
                });
            }

            return Json(new
            {
                totalProcessed = items.Count,
                successCount = successCount,
                failureCount = failureCount,
                results = results
            });
        }
    }
}