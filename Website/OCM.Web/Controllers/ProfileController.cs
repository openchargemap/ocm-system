using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.MVC.Models;

namespace OCM.MVC.Controllers
{
    public partial class ProfileController : BaseController
    {
        public static void UpdateCookie(HttpContext context, string cookieName, string cookieValue)
        {
            if (context.Request.Cookies.Keys.Contains(cookieName))
            {
                context.Response.Cookies.Delete(cookieName);
            }

            context.Response.Cookies.Append(cookieName, cookieValue, new CookieOptions { Expires = DateTime.Now.AddMonths(2) });
        }

        protected static void ClearCookie(HttpResponse response, string cookieName, string cookieValue)
        {
            response.Cookies.Delete(cookieName);
        }

        //
        // GET: /Profile/

        [Authorize(Roles = "StandardUser")]
        public ActionResult Index()
        {
            var userManager = new UserManager();
            var user = userManager.GetUser((int)UserID);

            return View(user);
        }

        public ActionResult SignIn(string redirectUrl)
        {
            return RedirectToAction("Index", "LoginProvider",
                                    new { _mode = "silent", _forceLogin = true, _redirectUrl = redirectUrl });
        }

        public ActionResult SignOut()
        {
            if (IsUserSignedIn)
            {
                // FIXME: need a token for app logins vs web logins as signout of web shouldn't be a sign out of app
                // assign fresh session token for next login
                var userManager = new UserManager();
                userManager.AssignNewSessionToken((int)UserID);
            }

            //clear cookies
            foreach (var cookieKey in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookieKey);
            }

            //clear session
            Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult View(int id)
        {
            var userManager = new UserManager();
            var user = userManager.GetUser(id);
            using (var refDataManager = new ReferenceDataManager())
            {
                ViewBag.CountryList = new SelectList(refDataManager.GetCountries(false), "ID", "Title");
            }
            return View(user);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult Edit()
        {
            var userManager = new UserManager();
            var user = userManager.GetUser((int)UserID);

            return View(user);
        }

        [Authorize(Roles = "StandardUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User updateProfile)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    var userManager = new UserManager();
                    var user = userManager.GetUser((int)UserID);

                    bool updatedOK = false;
                    if (user.ID == updateProfile.ID)
                    {
                        updatedOK = userManager.UpdateUserProfile(updateProfile, false);
                    }

                    if (updatedOK)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["UpdateFailed"] = true;
                    }

                }
                catch
                {
                    return View();
                }
            }
            return View(updateProfile);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult ChangePassword()
        {
            bool requireCurrentPassword = true;

            var userManager = new UserManager();
            //allow user to set a new password without confirming old one if they haven't set a password yet
            if (!userManager.HasPassword((int)UserID))
            {
                requireCurrentPassword = false;
            }

            if (TempData["IsCurrentPasswordRequired"] != null && (bool)TempData["IsCurrentPasswordRequired"] == false)
            {
                requireCurrentPassword = false;
            }
            return View(new PasswordChangeModel { IsCurrentPasswordRequired = requireCurrentPassword });
        }

        [Authorize(Roles = "StandardUser"), HttpPost, ValidateAntiForgeryToken]
        public ActionResult ChangePassword(API.Common.Model.PasswordChangeModel model)
        {
            if (ModelState.IsValid)
            {
                var passwordChanged = new UserManager().SetNewPassword((int)UserID, model);

                model.PasswordResetFailed = !passwordChanged;

                if (passwordChanged)
                {
                    model.PasswordResetCompleted = true;
                    return RedirectToAction("Index", "Profile");
                }
            }

            return View(model);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult Comments()
        {
            UserManager userManager = new UserManager();

            var user = userManager.GetUser((int)UserID);
            using (var commentManager = new UserCommentManager())
            {
                var list = commentManager.GetUserComments(user.ID).OrderByDescending(c => c.DateCreated);
                return View(list);
            }
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult CommentDelete(int id)
        {
            var user = new UserManager().GetUser((int)UserID);
            using (var commentManager = new UserCommentManager())
            {
                var list = commentManager.GetUserComments(user.ID);

                //delete comment if owned by this user
                if (list.Where(c => c.User.ID == user.ID && c.ID == id).Any())
                {
                    commentManager.DeleteComment(user.ID, id);
                }
            }

            return RedirectToAction("Comments");
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult Media()
        {
            UserManager userManager = new UserManager();

            var user = userManager.GetUser((int)UserID);
            var list = new MediaItemManager().GetUserMediaItems(user.ID).OrderByDescending(m => m.DateCreated);
            return View(list);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult MediaDelete(int id)
        {
            var user = new UserManager().GetUser((int)UserID);
            var itemManager = new MediaItemManager();
            var list = itemManager.GetUserMediaItems(user.ID);

            //delete item if owned by this user
            if (list.Where(c => c.User.ID == user.ID && c.ID == id).Any())
            {
                itemManager.DeleteMediaItem(user.ID, id);
            }

            return RedirectToAction("Media");
        }

        [Authorize(Roles = "StandardUser")]
        public async Task<ActionResult> Applications()
        {
            UserManager userManager = new UserManager();

            var user = userManager.GetUser((int)UserID);
            ViewBag.UserProfile = user;

            using (var apps = new RegisteredApplicationManager())
            {
                var summary = new ApplicationSummary();
                summary.RegisteredApplications = (await apps.Search(null, null, 1, 500, user.ID)).ToList();
                summary.AuthorizedApplications = apps.GetUserAuthorizedApplications(user.ID);
                return View(summary);
            }

        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult AppEdit(int? id)
        {
            var app = new RegisteredApplication();
            var userId = (int)UserID;

            if (id != null)
            {
                using (var appManager = new RegisteredApplicationManager())
                {
                    app = appManager.GetRegisteredApplication((int)id, userId);
                }

            }
            else
            {
                app.UserID = userId;
                app.IsEnabled = true;
                app.IsWriteEnabled = true;

            }

            return View("AppEdit", app);
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult AppGenerateNewKey(int? id)
        {
            var userId = (int)UserID;

            if (id != null)
            {
                using (var appManager = new RegisteredApplicationManager())
                {
                    var app = appManager.GetRegisteredApplication((int)id, userId);

                    if (app != null)
                    {
                        app = appManager.GenerateNewAPIKey((int)id, userId);
                    }
                }

            }

            return RedirectToAction("Applications");
        }


        [Authorize(Roles = "StandardUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AppEdit(RegisteredApplication app)
        {
            if (ModelState.IsValid)
            {

                using (var appManager = new RegisteredApplicationManager())
                {
                    app = appManager.UpdateRegisteredApplication(app, UserID);
                }
                return RedirectToAction("Applications", "Profile");
            }

            return View(app);
        }

    }
}