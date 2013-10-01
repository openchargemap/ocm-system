using DotNetOpenAuth.ApplicationBlock;
using OCM.API.Common;
using OCM.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OCM.MVC.Controllers
{
    public class LoginProviderController : Controller
    {

        public static void UpdateCookie(HttpResponseBase response, string cookieName, string cookieValue)
        {
            if (response.Cookies.AllKeys.Contains(cookieName))
            { 
                response.Cookies[cookieName].Value = cookieValue;
            }
            else
            {
                response.Cookies.Add(new HttpCookie(cookieName, cookieValue));
            }
        }

        public static string GetCookie(HttpRequestBase request, string cookieName)
        {
            if (request.Cookies.AllKeys.Contains(cookieName))
            {
                return request.Cookies[cookieName].Value;
            }
            else
            {
                return "";
            }
        }
        public static void ClearCookie(HttpResponseBase response, string cookieName, string cookieValue)
        {
            if (response.Cookies.AllKeys.Contains(cookieName))
            {
                response.Cookies[cookieName].Value = cookieValue;
                response.Cookies[cookieName].Expires = DateTime.Now.AddDays(-1);
            }
        }

        //
        // GET: /LoginProvider/

        public ActionResult Index(string _mode, bool? _forceLogin, string _redirectURL, string oauth_token, string denied)
        {

            //preserve url we're returning to after sign in
            if (!String.IsNullOrEmpty(_redirectURL))
            {
                Session["_redirectURL"] = _redirectURL;
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

            //if initiating a login, attempt to authenticate with twitter
            if (_mode == "silent" && String.IsNullOrEmpty(oauth_token))
            {
                //silently initiate pass through to twitter login
                TwitterConsumer.StartSignInWithTwitter((bool)_forceLogin).Send();
            }
            else
            {

                if (TwitterConsumer.IsTwitterConsumerConfigured)
                {

                    if (!String.IsNullOrEmpty(_redirectURL))
                    {
                        Session["_redirectURL"] = _redirectURL;
                    }

                    string screenName;
                    int userId;
                    if (TwitterConsumer.TryFinishSignInWithTwitter(out screenName, out userId))
                    {
                        string newSessionToken = Guid.NewGuid().ToString();


                        var dataModel = new OCMEntities();
                        var userDetails = dataModel.Users.FirstOrDefault(u => u.Identifier == screenName && u.IdentityProvider == "Twitter");
                        if (userDetails == null)
                        {
                            //create new user details   
                            userDetails = new Core.Data.User();
                            userDetails.IdentityProvider = "Twitter";
                            userDetails.Identifier = screenName;
                            userDetails.Username = screenName;
                            userDetails.DateCreated = DateTime.UtcNow;
                            userDetails.DateLastLogin = DateTime.UtcNow;

                            //only update session token if new (also done on logout)
                            if (String.IsNullOrEmpty(userDetails.CurrentSessionToken)) userDetails.CurrentSessionToken = newSessionToken;

                            dataModel.Users.Add(userDetails);
                        }
                        else
                        {
                            userDetails.DateLastLogin = DateTime.UtcNow;

                            //only update session token if new (also done on logout)
                            if (String.IsNullOrEmpty(userDetails.CurrentSessionToken)) userDetails.CurrentSessionToken = newSessionToken;
                        }

                        //get whichever session token we used
                        newSessionToken = userDetails.CurrentSessionToken;

                        dataModel.SaveChanges();

                        string permissions = (userDetails.Permissions != null ? userDetails.Permissions : "");
                        
                        UpdateCookie(Response, "IdentityProvider", "Twitter");
                        UpdateCookie(Response, "Identifier", screenName);
                        UpdateCookie(Response, "Username", userDetails.Username);
                        UpdateCookie(Response, "OCMSessionToken", newSessionToken);
                        //UpdateCookie("AccessToken", Request["oauth_token"]);
                        UpdateCookie(Response, "AccessPermissions", permissions);

                        Session["IdentityProvider"] = "Twitter";
                        Session["Identifier"] = screenName;
                        Session["Username"] = userDetails.Username;
                        Session["UserID"] = userDetails.ID;

                        if (UserManager.IsUserAdministrator(OCM.API.Common.Model.Extensions.User.FromDataModel(userDetails)))
                        {
                            Session["IsAdministrator"] = true;
                        }

                        if (!String.IsNullOrEmpty((string)Session["_redirectURL"]))
                        {
                            string returnURL = Session["_redirectURL"].ToString();
                            //Response.Redirect(returnURL);
                            return Redirect(returnURL);
                        }
                        else
                        {
                            //redirect to home page
                            return RedirectToAction("Index", "Home");
                        }
                    }

                }
            }

            return View();
        }

        public ActionResult AppLogin(bool? redirectWithToken)
        {
            if (redirectWithToken==true)
            { 
                var IdentityProvider  = GetCookie(Request, "IdentityProvider");
                var Identifier  = GetCookie(Request,"Identifier");
                var Username  = GetCookie(Request,"Username");
                var SessionToken  = GetCookie(Request,"OCMSessionToken");
                var Permissions  = GetCookie(Request,"Permissions");

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
    }
}
