using Microsoft.AspNetCore.Mvc;

namespace OCM.MVC.Controllers
{
    public class HomeController : BaseController
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
            return View("NotSignedIn");
        }

        public ActionResult Error(int code)
        {
            if (code >= 400 && code <= 500)
            {
                if (!IsUserSignedIn)
                {
                    return RedirectToAction("SignIn", "Profile");
                }

            }
            return View();
        }
    }
}
