using Microsoft.AspNetCore.Mvc;
using OCM.API.Common;
using OCM.API.Common.Model;

namespace OCM.MVC.Controllers
{
    [AuthSignedInOnly]
    public class EditQueueController : BaseController
    {
        //
        // GET: /EditQueue/

        public ActionResult Index(EditQueueFilter filter)
        {
            using (var editQueueManager = new EditQueueManager())
            {
                var list = editQueueManager.GetEditQueueItems(filter);
                ViewBag.EditFilter = filter;
                ViewBag.IsUserAdmin = IsUserAdmin;
                if (IsUserSignedIn)
                {
                    ViewBag.UserProfile = new UserManager().GetUser((int)UserID);
                }
                return View(list);
            }
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult Cleanup()
        {
            using (var editQueueManager = new EditQueueManager())
            {
                editQueueManager.CleanupRedundantEditQueueitems();

                return View();
            }
        }

        [AuthSignedInOnly]
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

        [AuthSignedInOnly]
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