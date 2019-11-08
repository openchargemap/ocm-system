using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using OCM.API.Common;
using OCM.Core.Data;

namespace OCM.MVC.Controllers
{
    public class LoginProviderController : BaseController
    {
        #region Cookie Helpers

        public static void UpdateCookie(HttpContext context, string cookieName, string cookieValue)
        {
            if (context.Request.Cookies.Keys.Contains(cookieName))
            {
                context.Response.Cookies.Delete(cookieName);
            }

            context.Response.Cookies.Append(cookieName, cookieValue, new CookieOptions { Expires = DateTime.Now.AddMonths(2) });
        }

        public static string GetCookie(HttpRequest request, string cookieName)
        {
            if (request.Cookies.Keys.Contains(cookieName))
            {
                return request.Cookies[cookieName];
            }
            else
            {
                return "";
            }
        }

        public static void ClearCookie(HttpResponse response, string cookieName, string cookieValue)
        {
            response.Cookies.Delete(cookieName);
        }

        #endregion Cookie Helpers

        #region Login Workflow Handlers


        private string ComputePayloadSig(string secret, string payload)
        {
            // from : https://github.com/laktak/discourse-sso/blob/master/src/Handler.cs
            var hash = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret)).ComputeHash(Encoding.UTF8.GetBytes(payload));
            return string.Join("", hash.Select(b => String.Format("{0:x2}", b)));
        }

        [Authorize(Roles = "StandardUser")]
        public ActionResult CommunitySSO(string sso, string sig)
        {
            // implement SSO for Discourse
            // https://meta.discourse.org/t/official-single-sign-on-for-discourse-sso/13045

            var secret = ConfigurationManager.AppSettings["CommunitySSOSecret"];

            var computedSig = ComputePayloadSig(secret, sso);

            if (sig != computedSig)
            {
                // secrets don't match;
                return Unauthorized();
            }

            string decodedSso = Encoding.UTF8.GetString(Convert.FromBase64String(sso));
            var args = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(decodedSso);
            var nonce = args["nonce"];

            if (IsUserSignedIn)
            {
                var userManager = new UserManager();
                var user = userManager.GetUser((int)UserID);

                // create payload, base64 encode & url encode
                string returnPayload = $"nonce={nonce}&email={user.EmailAddress}&require_activation=true&external_id={user.ID}&username={user.Username}";

                string base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(returnPayload));
                string urlEncodedPayload = Uri.EscapeUriString(base64Payload);
                string returnSig = ComputePayloadSig(secret, urlEncodedPayload);

                // send auth request back to Discourse
                string redirectTo = $"{ConfigurationManager.AppSettings["CommunityUrl"]}/session/sso_login?sso={urlEncodedPayload}&sig={returnSig}";
                return Redirect(redirectTo);

            } else
            {
                return Unauthorized();
            }
        }

        public ActionResult BeginLogin()
        {
            ViewBag.IsReadOnlyMode = this.IsReadOnlyMode;

            return View(new OCM.API.Common.Model.LoginModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult BeginLogin(OCM.API.Common.Model.LoginModel loginModel)
        {
            if (ModelState.IsValid)
            {
                var userManager = new UserManager();

                try
                {
                    var user = userManager.GetUser(loginModel);
                    if (user != null)
                    {
                        return ProcessLoginResult(user.Identifier, user.IdentityProvider, user.Username, user.EmailAddress);
                    }
                    else
                    {
                        ViewBag.InvalidLogin = true;
                    }
                }
                catch (UserManager.PasswordNotSetException)
                {
                    return RedirectToAction("PasswordReset", new { emailAddress = loginModel.EmailAddress });
                }
            }

            return View(loginModel);
        }

        public ActionResult PasswordReset(string emailAddress)
        {
            return View(new OCM.API.Common.Model.PasswordResetRequestModel { EmailAddress = emailAddress });
        }

        [HttpPost]
        public ActionResult PasswordReset(OCM.API.Common.Model.PasswordResetRequestModel model)
        {
            if (ModelState.IsValid)
            {
                //send confirmation email
                bool resetInitiated = new UserManager().BeginPasswordReset(model.EmailAddress);
                if (resetInitiated)
                {
                    model.ResetInitiated = true;
                }
                else
                {
                    model.ResetInitiated = false;
                    model.IsUnknownAccount = true;
                }
            }
            return View(model);
        }

        public ActionResult ConfirmPasswordReset(string token, string email)
        {
            //check token is valid for email, then sign in user and go to password change
            var userManager = new UserManager();

            var user = userManager.GetUserFromResetToken(email, token);

            if (user != null)
            {
                userManager.AssignNewSessionToken(user.ID, true);

                //sign in user
                PerformCoreLogin(user);

                //proceed to password change
                TempData["IsCurrentPasswordRequired"] = false;

                return RedirectToAction("ChangePassword", "Profile");
            }

            return View();
        }

        public ActionResult Register()
        {
            return View(new OCM.API.Common.Model.RegistrationModel { IsCurrentPasswordRequired = false });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(OCM.API.Common.Model.RegistrationModel model)
        {
            if (ModelState.IsValid)
            {
                //register as new user, check email is valid first
                var userManager = new UserManager();
                var user = userManager.RegisterNewUser(model);

                if (user != null)
                {
                    return RedirectToAction("BeginLogin", "LoginProvider");
                }
                else
                {
                    model.RegistrationFailed = true;
                }
            }

            return View(model);
        }

        public ActionResult LoginFailed()
        {
            TempData["LoginFailed"] = true;

            return RedirectToAction("BeginLogin");
        }

        [AllowAnonymous]
        public void LoginWithProvider(string provider)
        {
            //OAuthWebSecurity.RequestAuthentication(provider, Url.Action("AuthenticationCallback"));
        }

        [AllowAnonymous]
        public ActionResult AuthenticationCallback()
        {
            /* try
             {
                 //http://brockallen.com/2012/09/04/using-oauthwebsecurity-without-simplemembership/
                 var result = OAuthWebSecurity.VerifyAuthentication();
                 if (result.IsSuccessful)
                 {
                     // name of the provider we just used
                     var provider = result.Provider;
                     // provider's unique ID for the user
                     var uniqueUserID = result.ProviderUserId;
                     // since we might use multiple identity providers, then
                     // our app uniquely identifies the user by combination of
                     // provider name and provider user id
                     var uniqueID = provider + "/" + uniqueUserID;

                     // we then log the user into our application
                     // we could have done a database lookup for a
                     // more user-friendly username for our app
                     //FormsAuthentication.SetAuthCookie(uniqueID, false);

                     // dictionary of values from identity provider
                     var userDataFromProvider = result.ExtraData;

                     string email = null;
                     string name = null;

                     if (userDataFromProvider.ContainsKey("email")) email = userDataFromProvider["email"];

                     if (userDataFromProvider.ContainsKey("name")) name = userDataFromProvider["name"];

                     //for legacy reasons, with twitter we use the text username instead of numeric userid
                     if (provider == "twitter" && !String.IsNullOrEmpty(result.UserName)) uniqueUserID = result.UserName;

                     return ProcessLoginResult(uniqueUserID, provider, name, email);
                 }
             }
             catch (Exception exp)
             {
                 System.Diagnostics.Debug.WriteLine(exp.ToString());
             }
             */
            return RedirectToAction("LoginFailed");
        }

        // Legacy entry point/endpoint for oauth logins
        // GET: /LoginProvider/

        public ActionResult Index(string _mode, bool? _forceLogin, string _redirectURL, string oauth_token, string denied)
        {
            //preserve url we're returning to after sign in
            if (!String.IsNullOrEmpty(_redirectURL))
            {
                HttpContext.Session.SetString("_redirectURL", _redirectURL);
            }

            //if denied access, redirect to normal redirect url
            if (denied != null)
            {
                if (_redirectURL == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return Redirect(_redirectURL);
                }
            }

            return RedirectToAction("BeginLogin");
        }

        private ActionResult ProcessLoginResult(string userIdentifier, string loginProvider, string name, string email)
        {
            //TODO: move all of this logic to UserManager
            string newSessionToken = Guid.NewGuid().ToString();

            var dataModel = new OCMEntities();
            var userDetails = dataModel.Users.FirstOrDefault(u => u.Identifier.ToLower() == userIdentifier.ToLower() && u.IdentityProvider.ToLower() == loginProvider.ToLower());
            if (userDetails == null)
            {
                //create new user details
                userDetails = new Core.Data.User();
                userDetails.IdentityProvider = loginProvider;
                userDetails.Identifier = userIdentifier;
                if (String.IsNullOrEmpty(name) && loginProvider.ToLower() == "twitter") name = userIdentifier;
                if (String.IsNullOrEmpty(name) && email != null) name = email.Substring(0, email.IndexOf("@"));

                userDetails.Username = name;
                userDetails.EmailAddress = email;
                userDetails.DateCreated = DateTime.UtcNow;
                userDetails.DateLastLogin = DateTime.UtcNow;
                userDetails.IsProfilePublic = true;

                //only update session token if new (also done on logout)
                if (String.IsNullOrEmpty(userDetails.CurrentSessionToken)) userDetails.CurrentSessionToken = newSessionToken;

                dataModel.Users.Add(userDetails);
            }
            else
            {
                //update date last logged in and refresh users details if more information provided
                userDetails.DateLastLogin = DateTime.UtcNow;

                if (userDetails.Username == userDetails.Identifier && !String.IsNullOrEmpty(name)) userDetails.Username = name;
                if (String.IsNullOrEmpty(userDetails.EmailAddress) && !String.IsNullOrEmpty(email)) userDetails.EmailAddress = email;

                //only update session token if new (also done on logout)
                if (String.IsNullOrEmpty(userDetails.CurrentSessionToken)) userDetails.CurrentSessionToken = newSessionToken;
            }

            //get whichever session token we used
            //newSessionToken = userDetails.CurrentSessionToken;

            //store updates to user
            dataModel.SaveChanges();

            PerformCoreLogin(OCM.API.Common.Model.Extensions.User.FromDataModel(userDetails));
            /*if (HttpContext.Items.ContainsKey("_postLoginRedirect"))
            {
                string returnURL = HttpContext.Items["_postLoginRedirect"].ToString();
                return Redirect(returnURL);
            }*/

            if (!String.IsNullOrEmpty(Session.GetString("_redirectURL")))
            {
                string returnURL = Session.GetString("_redirectURL");
                return Redirect(returnURL);
            }
            else
            {
                //nowhere specified to redirect to, redirect to home page
                return RedirectToAction("Index", "Home");
            }
        }

        public void PerformCoreLogin(OCM.API.Common.Model.User userDetails)
        {
            string permissions = (userDetails.Permissions != null ? userDetails.Permissions : "");

            var session = Session;

            UpdateCookie(HttpContext, "IdentityProvider", userDetails.IdentityProvider);
            UpdateCookie(HttpContext, "Identifier", userDetails.Identifier);
            UpdateCookie(HttpContext, "Username", userDetails.Username);
            UpdateCookie(HttpContext, "OCMSessionToken", userDetails.CurrentSessionToken);
            UpdateCookie(HttpContext, "AccessPermissions", permissions);

            session.SetString("IdentityProvider", userDetails.IdentityProvider);
            session.SetString("Identifier", userDetails.Identifier);
            session.SetString("Username", userDetails.Username);
            session.SetInt32("UserID", userDetails.ID);

            if (UserManager.IsUserAdministrator(userDetails))
            {
                session.SetString("IsAdministrator", "true");
            }
        }

        public ActionResult AppLogin(bool? redirectWithToken)
        {
            if (redirectWithToken == true)
            {
                var IdentityProvider = GetCookie(Request, "IdentityProvider");
                var Identifier = GetCookie(Request, "Identifier");
                var Username = GetCookie(Request, "Username");
                var SessionToken = GetCookie(Request, "OCMSessionToken");
                var Permissions = GetCookie(Request, "Permissions");

                return RedirectToAction("AppLogin", new
                {
                    IdentityProvider = IdentityProvider,
                    Identifier = Identifier,
                    OCMSessionToken = SessionToken,
                    Permissions = Permissions
                });
            }
            else
            {
                return View();
            }
        }

        #endregion Login Workflow Handlers
    }
}