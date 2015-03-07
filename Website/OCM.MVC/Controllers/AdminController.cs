using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.Core.Data;
using OCM.Import.Providers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace OCM.MVC.Controllers
{
    public class AdminController : AsyncController
    {
        //
        // GET: /Admin/
        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult Users()
        {
            var userList = new UserManager().GetUsers().OrderByDescending(u => u.DateCreated);
            return View(userList);
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult EditUser(int id)
        {
            var user = new UserManager().GetUser(id);
            return View(user);
        }

        [AuthSignedInOnly(Roles = "Admin")]
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

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult PromoteUserToEditor(int userId, int countryId, bool autoCreateSubscriptions, bool removePermission)
        {
            new UserManager().PromoteUserToCountryEditor(int.Parse(Session["UserID"].ToString()), userId, countryId, autoCreateSubscriptions, removePermission);
            return RedirectToAction("View", "Profile", new { id = userId });
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult ConvertPermissions()
        {
            //convert all user permission to new format where applicable
            new UserManager().ConvertUserPermissions();
            return RedirectToAction("Index", "Admin", new { result = "processed" });
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult Operators()
        {
            var operatorInfoManager = new OperatorInfoManager();

            return View(operatorInfoManager.GetOperators());
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult EditOperator(int? id)
        {
            var operatorInfo = new OperatorInfo();

            if (id != null) operatorInfo = new OperatorInfoManager().GetOperatorInfo((int)id);
            return View(operatorInfo);
        }

        [AuthSignedInOnly(Roles = "Admin")]
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

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult CommentDelete(int id)
        {
            var commentManager = new UserCommentManager();
            var user = new UserManager().GetUser(int.Parse(Session["UserID"].ToString()));
            commentManager.DeleteComment(user.ID, id);
            return RedirectToAction("Index");
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult MediaDelete(int id)
        {
            var itemManager = new MediaItemManager();
            var user = new UserManager().GetUser(int.Parse(Session["UserID"].ToString()));
            itemManager.DeleteMediaItem(user.ID, id);
            return RedirectToAction("Details", "POI");
        }

        public async Task<JsonResult> PollForTasks(string key)
        {
            int notificationsSent = 0;
            MirrorStatus mirrorStatus = null;
            //poll for periodic tasks (subscription notifications etc)
            if (key == System.Configuration.ConfigurationManager.AppSettings["AdminPollingAPIKey"])
            {
                //send all pending subscription notifications
                try
                {
                    
                    //TODO: can't run in seperate async thread becuase HttpContext is not available
                    string templateFolderPath = Server.MapPath("~/templates/notifications");

                    await Task.Run(() =>
                    {
                        notificationsSent = new UserSubscriptionManager().SendAllPendingSubscriptionNotifications(templateFolderPath);
                    });
                }
                catch (Exception)
                {
                    ; ; //failed to send notifications
                }

                //update cache mirror
                try
                {
                    HttpContext.Application["_MirrorRefreshInProgress"] = true;
                    mirrorStatus = await CacheManager.RefreshCachedData(CacheUpdateStrategy.Modified);
                }
                catch (Exception)
                {
                    ; ;//failed to update cache
                }
                finally
                {
                    HttpContext.Application["_MirrorRefreshInProgress"] = false;
                }
            }
            return Json(new { NotificationsSent = notificationsSent, MirrorStatus = mirrorStatus }, JsonRequestBehavior.AllowGet);
        }

        [AuthSignedInOnly(Roles = "Admin")]
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
                if (HttpContext.Application["_MirrorRefreshInProgress"] != null && (bool)HttpContext.Application["_MirrorRefreshInProgress"] == true)
                {
                    status.Description += " (Update in progress)";
                }
            }
            return View(status);
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public async Task<JsonResult> RefreshPOIMirror(string mode)
        {
            MirrorStatus status = new MirrorStatus();

            if (HttpContext.Application["_MirrorRefreshInProgress"] == null || (bool)HttpContext.Application["_MirrorRefreshInProgress"] == false)
            {
                HttpContext.Application["_MirrorRefreshInProgress"] = true;

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

                HttpContext.Application["_MirrorRefreshInProgress"] = false;
            }
            else
            {
                status.StatusCode = System.Net.HttpStatusCode.PartialContent;
                status.Description = "Update currently in progress";
            }

            return Json(status, JsonRequestBehavior.AllowGet);
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult ImportManager()
        {
            var importManager = new Import.ImportManager(Server.MapPath("~/Temp"));
            var providers = importManager.GetImportProviders(new ReferenceDataManager().GetDataProviders());
            var model = new Models.ImportManager() { ImportProviders = providers };

            return View(model);
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public async Task<ActionResult> Import(string providerName, bool fetchLiveData, bool performImport = false)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var importManager = new Import.ImportManager(Server.MapPath("~/Temp"));

            var providers = importManager.GetImportProviders(new ReferenceDataManager().GetDataProviders());
            var provider = providers.FirstOrDefault(p => p.GetProviderName() == providerName);
            var coreReferenceData = new ReferenceDataManager().GetCoreReferenceData();
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

            stopwatch.Stop();
            result.Log += "\r\nImport processing time (seconds): " + stopwatch.Elapsed.TotalSeconds;

            return View(result);
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult ConfigCheck()
        {
            return View();
        }
    }
}