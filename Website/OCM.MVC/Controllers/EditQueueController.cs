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

        [AuthSignedInOnly]
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
            //approves/publishes the given edit directly
            using (var editQueueManager = new EditQueueManager())
            {
                editQueueManager.ProcessEditQueueItem(id, true, (int)Session["UserID"]);

                return RedirectToAction("Index", "EditQueue");
            }
        }

        [AuthSignedInOnly]
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

       
        //
        // GET: /EditQueue/Edit/5

        public ActionResult Edit(int id)
        {
            return View();
        }

        //
        // POST: /EditQueue/Edit/5

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /EditQueue/Delete/5

        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /EditQueue/Delete/5

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
