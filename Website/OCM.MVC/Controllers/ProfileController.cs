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
                response.Cookies[cookieName].Expires = DateTime.UtcNow.AddDays(-1);
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

        [AuthSignedInOnly]
        public ActionResult Comments()
        {
           
            UserManager userManager = new UserManager();

            var user = userManager.GetUser(int.Parse(Session["UserID"].ToString()));
            var list = new UserCommentManager().GetUserComments(user.ID);
            return View(list);
           
        }

        [AuthSignedInOnly]
        public ActionResult CommentDelete(int id)
        {
            var user = new UserManager().GetUser(int.Parse(Session["UserID"].ToString()));
            var commentManager = new UserCommentManager();
            var list = commentManager.GetUserComments(user.ID);

            //delete comment if owned by this user
            if (list.Where(c=>c.User.ID ==user.ID && c.ID==id).Any())
            {
                commentManager.DeleteComment(user.ID, id);
            }


            return RedirectToAction("Comments");
        }

        [AuthSignedInOnly]
        public ActionResult Media()
        {

            UserManager userManager = new UserManager();

            var user = userManager.GetUser(int.Parse(Session["UserID"].ToString()));
            var list = new MediaItemManager().GetUserMediaItems(user.ID);
            return View(list);

        }

        [AuthSignedInOnly]
        public ActionResult MediaDelete(int id)
        {
            var user = new UserManager().GetUser(int.Parse(Session["UserID"].ToString()));
            var itemManager = new MediaItemManager();
            var list = itemManager.GetUserMediaItems(user.ID);

            //delete item if owned by this user
            if (list.Where(c => c.User.ID == user.ID && c.ID == id).Any())
            {
                itemManager.DeleteMediaItem(user.ID, id);
            }

            return RedirectToAction("Media");
        }
    }
}

