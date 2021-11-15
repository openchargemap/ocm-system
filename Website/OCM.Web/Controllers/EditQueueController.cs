﻿using Microsoft.AspNetCore.Mvc;
using OCM.API.Common;
using OCM.API.Common.Model;
using System.Threading.Tasks;

namespace OCM.MVC.Controllers
{
    [Authorize(Roles = "StandardUser")]
    public class EditQueueController : BaseController
    {
        //
        // GET: /EditQueue/

        public async Task<ActionResult> Index(EditQueueFilter filter)
        {
            using (var editQueueManager = new EditQueueManager())
            {
                var list = await editQueueManager.GetEditQueueItems(filter);
                ViewBag.EditFilter = filter;
                ViewBag.IsUserAdmin = IsUserAdmin;
                if (IsUserSignedIn)
                {
                    ViewBag.UserProfile = new UserManager().GetUser((int)UserID);
                }
                return View(list);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Cleanup()
        {
            using (var editQueueManager = new EditQueueManager())
            {
                await editQueueManager.CleanupRedundantEditQueueitems();

                return RedirectToAction("Index", "EditQueue");
            }
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult Publish(int id)
        {
            //approves/publishes the given edit directly (if user has permission)
            using (var editQueueManager = new EditQueueManager())
            {
                if (!IsReadOnlyMode)
                {
                    editQueueManager.ProcessEditQueueItem(id, true, (int)UserID);
                }
                return RedirectToAction("Index", "EditQueue");
            }
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult MarkAsProcessed(int id)
        {
            //marks item as processed without publishing the edit
            using (var editQueueManager = new EditQueueManager())
            {
                if (!IsReadOnlyMode)
                {
                    editQueueManager.ProcessEditQueueItem(id, false, (int)UserID);
                }
                return RedirectToAction("Index", "EditQueue");
            }
        }

        //
        // GET: /EditQueue/Details/5

        public ActionResult Details(int id)
        {
            return View();
        }
    }
}