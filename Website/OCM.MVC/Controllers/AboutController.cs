using OCM.API.Common;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OCM.MVC.Controllers
{
    public class AboutController : Controller
    {
        //
        // GET: /About/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Terms()
        {
            return View();
        }

        /// <summary>
        /// /Contact
        /// </summary>
        /// <returns></returns>
        public ActionResult Contact()
        {
            return View();
        }

        public ActionResult Guidance()
        {
            return View();
        }

        public ActionResult Funding()
        {
            return View();
        }
        /// <summary>
        /// /contact/ post new contact us mesage
        /// </summary>
        /// <param name="album"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Contact(ContactSubmission contactSubmission)
        {
            if (ModelState.IsValid)
            {
                SubmissionManager submissionManager = new SubmissionManager();
                bool sentOK = submissionManager.SubmitContactSubmission(contactSubmission);

                if (!sentOK)
                {
                    ViewBag.ErrorMessage = "There was a problem sending your message";
                }

                return RedirectToAction("ContactSubmitted");
            }
            
            return View(contactSubmission);
        }

        /// <summary>
        /// view shown after contact message submitted
        /// </summary>
        /// <returns></returns>
        public ActionResult ContactSubmitted()
        {
            return View();
        }
    }
}
