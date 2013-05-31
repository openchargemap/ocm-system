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

        public ActionResult Cleanup()
        {
            using (var editQueueManager = new EditQueueManager())
            {
               editQueueManager.CleanupRedundantEditQueueitems();

                return View();
            }
        }
        //
        // GET: /EditQueue/Details/5

        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /EditQueue/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /EditQueue/Create

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
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
