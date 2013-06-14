using OCM.API.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OCM.API.Common.Model;

namespace OCM.MVC.Controllers
{
    public class EditQueueController : Controller
    {
        //
        // GET: /EditQueue/

        public ActionResult Index(EditQueueFilter filter)
        {    
            using (var editQueueManager = new EditQueueManager())
            {
                var list = editQueueManager.GetEditQueueItems(filter);
                ViewBag.EditFilter = filter;

                return View(list);
            }
        }

        [AuthSignedInOnly(Roles="Admin")]
        public ActionResult Cleanup()
        {
            using (var editQueueManager = new EditQueueManager())
            {
               editQueueManager.CleanupRedundantEditQueueitems();

                return View();
            }
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult Publish(int id)
        {
            //approves/publishes the given edit directly
            using (var editQueueManager = new EditQueueManager())
            {
                editQueueManager.ProcessEditQueueItem(id, true, (int)Session["UserID"]);

                return RedirectToAction("Index", "EditQueue");
            }
        }

        [AuthSignedInOnly(Roles = "Admin")]
        public ActionResult MarkAsProcessed(int id)
        {
            //approves/publishes the given edit directly
            using (var editQueueManager = new EditQueueManager())
            {
                editQueueManager.ProcessEditQueueItem(id, false, (int)Session["UserID"]);

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
