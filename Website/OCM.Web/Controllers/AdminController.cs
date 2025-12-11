using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.API.Utils;
using OCM.Core.Data;
using OCM.Web.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.MVC.Controllers
{
    public class AdminController : BaseController
    {
        private IHostEnvironment _host;
        private IMemoryCache _cache;
        private IAdminTaskService _adminTaskService;

        public AdminController(IHostEnvironment host, IMemoryCache memoryCache, IAdminTaskService adminTaskService)
        {
            _host = host;
            _cache = memoryCache;
            _adminTaskService = adminTaskService;
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