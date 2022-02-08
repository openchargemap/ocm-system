using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using OCM.API.Common;
using OCM.API.Common.Model;
using System;

namespace OCM.MVC.Controllers
{
    public class AboutController : BaseController
    {
        IWebHostEnvironment _host;

        //
        // GET: /About/

        public AboutController(IWebHostEnvironment host)
        {
            _host = host;
        }

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

        public ActionResult DataSharing()
        {
            var countryList = new ReferenceDataManager().GetCountries(false);
            ViewBag.CountryList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(countryList, "ID", "Title");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "StandardUser")]
        public ActionResult DataSharing(DataSharingAgreement agreement)
        {

            var countryList = new ReferenceDataManager().GetCountries(false);
            ViewBag.CountryList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(countryList, "ID", "Title");

            if (ModelState.IsValid)
            {

                var submissionManager = new SubmissionManager();
                bool savedOK = submissionManager.SubmitDataAgreement(agreement, (int)UserID);

                if (!savedOK)
                {
                    ViewBag.ErrorMessage = "Please check you have filled out all of the fields. All information is required.";
                } else
                {
                    ViewBag.Submitted = true;
                }


            }

            return View(agreement);
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
                //check for spam bot by discarding submissions which populate phone field
                if (String.IsNullOrEmpty(contactSubmission.Phone))
                {
                    SubmissionManager submissionManager = new SubmissionManager();
                    bool sentOK = submissionManager.SubmitContactSubmission(contactSubmission);

                    if (!sentOK)
                    {
                        ViewBag.ErrorMessage = "There was a problem sending your message";
                    }
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