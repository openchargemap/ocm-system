using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using System.Collections.Generic;
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

                //results of a revert performed on the previous request
                if (TempData["RevertResults"] != null)
                {
                    ViewBag.RevertResults = JsonConvert.DeserializeObject<List<EditQueueRevertResult>>(TempData["RevertResults"].ToString());
                }

                return View(list);
            }
        }

        /// <summary>
        /// Revert one or more previously approved edits, restoring the content each edit replaced. Administrator only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> RevertSelected(int[] selectedItems, EditQueueFilter filter)
        {
            var currentUser = GetCurrentUserProfile();
            if (IsUserEditingBlocked(currentUser)) return EditingBlockedView(currentUser);

            if (IsReadOnlyMode)
            {
                TempData["ErrorMessage"] = "Service is currently read-only, edits cannot be reverted.";
            }
            else if (selectedItems == null || selectedItems.Length == 0)
            {
                TempData["ErrorMessage"] = "Select one or more approved edits to revert.";
            }
            else
            {
                using (var editQueueManager = new EditQueueManager())
                {
                    var results = await editQueueManager.RevertEditQueueItems(selectedItems, (int)UserID);
                    TempData["RevertResults"] = JsonConvert.SerializeObject(results);
                }
            }

            //return to the same view of the queue the revert was requested from
            return RedirectToAction(nameof(Index), new
            {
                filter.ShowEditsOnly,
                filter.ShowProcessed,
                filter.DateFrom,
                filter.DateTo,
                filter.ID,
                filter.UserId,
                filter.MinimumDifferences,
                filter.MaxResults
            });
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
            var currentUser = GetCurrentUserProfile();
            if (IsUserEditingBlocked(currentUser)) return EditingBlockedView(currentUser);

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
            var currentUser = GetCurrentUserProfile();
            if (IsUserEditingBlocked(currentUser)) return EditingBlockedView(currentUser);

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