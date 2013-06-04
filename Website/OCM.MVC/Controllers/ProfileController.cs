using OCM.API.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OCM.API.Common.Model;

namespace OCM.MVC.Controllers
{
    public class ProfileController : Controller
    {
        //
        // GET: /Profile/

        [AuthSignedInOnly]
        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                UserManager userManager = new UserManager();
                var user = userManager.GetUser(int.Parse(Session["UserID"].ToString()));

                return View(user);
            }
            return View();
        }

        public ActionResult SignIn(string redirectUrl)
        {
            return RedirectToAction("Index", "LoginProvider",
                                    new { _mode = "silent", _forceLogin = true, _redirectUrl = redirectUrl });
        }

        public ActionResult SignOut()
        {
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        [AuthSignedInOnly]
        public ActionResult Edit()
        {
            if (Session["UserID"] != null)
            {
                UserManager userManager = new UserManager();
                var user = userManager.GetUser(int.Parse(Session["UserID"].ToString()));

                return View(user);
            }
            else return View();
        }

        [AuthSignedInOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User updateProfile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (Session["UserID"] != null)
                    {
                        // TODO: Add update logic here
                        var userManager = new UserManager();
                        var user = userManager.GetUser((int)Session["UserID"]);
                        user.Username = updateProfile.Username;
                        user.Profile = updateProfile.Profile;
                        user.Location = updateProfile.Location;
                        user.WebsiteURL = updateProfile.WebsiteURL;
                        user.IsProfilePublic = updateProfile.IsProfilePublic;
                        user.IsPublicChargingProvider = updateProfile.IsPublicChargingProvider;
                        user.IsEmergencyChargingProvider = updateProfile.IsEmergencyChargingProvider;
                        user.EmailAddress = updateProfile.EmailAddress;
                        user.Latitude = updateProfile.Latitude;
                        user.Longitude = updateProfile.Longitude;

                        userManager.UpdateUserProfile(user);
                        return RedirectToAction("Index");
                    }
                    else return View();
                }
                catch
                {
                    return View();
                }
            }
            return View(updateProfile);
        }
    }
}

