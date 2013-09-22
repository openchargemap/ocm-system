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

        protected static void UpdateCookie(HttpResponseBase response, string cookieName, string cookieValue)
        {
            response.Cookies[cookieName].Value = cookieValue;
        }

        protected static void ClearCookie(HttpResponseBase response, string cookieName, string cookieValue)
        {
            if (response.Cookies.AllKeys.Contains(cookieName))
            {
                response.Cookies[cookieName].Value = cookieValue;
                response.Cookies[cookieName].Expires = DateTime.Now.AddDays(-1);
            }
        }

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
            if (Session["UserID"] != null)
            {
                // assign fresh session token for next login
                var userManager = new UserManager();
                userManager.AssignNewSessionToken((int)Session["UserID"]);
            }

            //clear cookies & set new session token
            UpdateCookie(Response, "IdentityProvider", "");
            UpdateCookie(Response, "Identifier", "");
            UpdateCookie(Response, "Username", "");
            UpdateCookie(Response, "OCMSessionToken", "");
            UpdateCookie(Response, "AccessPermissions", "");

            //clear session
            Session.Abandon();

            return RedirectToAction("Index", "Home");
        }

        [AuthSignedInOnly]
        public ActionResult View(int id)
        {
            UserManager userManager = new UserManager();
            var user = userManager.GetUser(id);
            return View(user);
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
                      
                        if (user.ID==updateProfile.ID)
                        { 
                            userManager.UpdateUserProfile(updateProfile, false);
                        }
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

