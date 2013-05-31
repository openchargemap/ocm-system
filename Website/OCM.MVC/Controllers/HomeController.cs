using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
    }
}
