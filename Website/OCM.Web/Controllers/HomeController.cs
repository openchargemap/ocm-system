using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace OCM.MVC.Controllers
{
    public class HomeController : Controller
    {

        /// <summary>
        /// Home page
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            ViewBag.IsHome = true;
            ViewBag.WideContainer = true; //tell layout to user wide view
            return View();
        }

        /// <summary>
        /// Access denied
        /// </summary>
        /// <returns></returns>
        public ActionResult NotSignedIn()
        {
            return View();
        }

        public ActionResult GeneralError()
        {
            return View();
        }
    }
}
