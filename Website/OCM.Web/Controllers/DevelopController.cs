using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace OCM.MVC.Controllers
{
    public class DevelopController : Controller
    {
        //
        // GET: /Develop/

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// /Develop/Apps
        /// </summary>
        /// <returns></returns>
        public ActionResult Apps()
        {
            return View();
        }

        /// <summary>
        /// Show API Docs
        /// </summary>
        /// <returns></returns>
        public ActionResult API()
        {
            return View();
        }
    }
}
