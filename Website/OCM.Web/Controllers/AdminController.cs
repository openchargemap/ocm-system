using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.Core.Data;
using OCM.Import.Providers;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using OCM.API.Utils;

namespace OCM.MVC.Controllers
{
    public class AdminController : BaseController
    {
        private IHostEnvironment _host;
        private IMemoryCache _cache;

        public AdminController(IHostEnvironment host, IMemoryCache memoryCache)
        {
            _host = host;
            _cache = memoryCache;
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
            //convert all user permission to new format where applicable
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

                operatorInfo = operatorInfoManager.UpdateOperatorInfo(operatorInfo);

                CacheManager.RefreshCachedData();
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
            int notificationsSent = 0;
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
            return Json(new { NotificationsSent = notificationsSent, MirrorStatus = mirrorStatus });
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
        public ActionResult ImportManager()
        {
            var tempPath = Path.GetTempPath();

            var importManager = new Import.ImportManager(new OCM.Import.ImportSettings { TempFolderPath = tempPath });
            using (var refDataManager = new ReferenceDataManager())
            {
                var providers = importManager.GetImportProviders(refDataManager.GetDataProviders());
                var model = new Models.ImportManager() { ImportProviders = providers };

                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Import(string providerName, bool fetchLiveData, bool performImport = false, bool includeResults = true)
        {
            GC.Collect();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var tempPath = Path.GetTempPath();

            var importManager = new Import.ImportManager(new Import.ImportSettings { TempFolderPath = tempPath });

            using (var refDataManager = new ReferenceDataManager())
            {
                var providers = importManager.GetImportProviders(refDataManager.GetDataProviders());
                var provider = providers.FirstOrDefault(p => p.GetProviderName() == providerName);
                var coreReferenceData = refDataManager.GetCoreReferenceData(new APIRequestParams());

                ((BaseImportProvider)provider).InputPath = importManager.TempFolder + "//cache_" + provider.GetProviderName() + ".dat";
                var result = await importManager.PerformImport(OCM.Import.Providers.ExportType.POIModelList, fetchLiveData, new OCM.API.Client.APICredentials(), coreReferenceData, "", provider, true);

                var systemUser = new UserManager().GetUser((int)StandardUsers.System);
                if (performImport)
                {
                    //add/update/delist POIs
                    await Task.Run(() =>
                    {
                        importManager.UpdateImportedPOIList(result, systemUser);
                    });
                }

                if (!includeResults)
                {
                    result.Added = new System.Collections.Generic.List<API.Common.Model.ChargePoint>();
                    result.Delisted = new System.Collections.Generic.List<API.Common.Model.ChargePoint>();
                    result.Duplicates = new System.Collections.Generic.List<API.Common.Model.ChargePoint>();
                    result.ImportItems = new System.Collections.Generic.List<Import.ImportItem>();
                    result.LowDataQuality = new System.Collections.Generic.List<API.Common.Model.ChargePoint>();
                    result.Unchanged = new System.Collections.Generic.List<API.Common.Model.ChargePoint>();
                    result.Updated = new System.Collections.Generic.List<API.Common.Model.ChargePoint>();
                }
                stopwatch.Stop();
                result.Log += "\r\nImport processing time (seconds): " + stopwatch.Elapsed.TotalSeconds;

                return View(result);
            }

        }

        [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> ImportUpload(FormCollection collection)
        {
            var providerName = Request.Form["providerName"];

            var tempPath = Path.GetTempPath();

            var importManager = new Import.ImportManager(new OCM.Import.ImportSettings { TempFolderPath = tempPath });
            var providers = importManager.GetImportProviders(new ReferenceDataManager().GetDataProviders());
            var provider = providers.FirstOrDefault(p => p.GetProviderName() == Request.Form["providerName"]);

            var uploadFilename = importManager.TempFolder + "//cache_" + provider.GetProviderName() + ".dat";

            var postedFile = Request.Form.Files[0];

            if (postedFile != null && postedFile.Length > 0)
            {
                //store upload
                using (var stream = System.IO.File.Create(uploadFilename))
                {
                    await postedFile.CopyToAsync(stream);
                }

                //redirect to import processing (cached file version)
                return RedirectToAction("Import", new { providerName = providerName, fetchLiveData = false, performImport = false });
            }

            //faileds
            return RedirectToAction("ImportManager");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ConfigCheck()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Benchmarks()
        {
            CacheProviderMongoDB cache = new CacheProviderMongoDB();
            var results = cache.PerformPOIQueryBenchmark(10, "distance");
            return View(results);
        }
    }
}