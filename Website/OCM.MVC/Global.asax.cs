using OCM.API.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace OCM.MVC
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        #region Cookie Helpers
        public static void UpdateCookie(System.Web.HttpResponseBase response, string cookieName, string cookieValue)
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

        public static string GetCookie(System.Web.HttpRequestBase request, string cookieName)
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

        public static void ClearCookie(System.Web.HttpResponseBase response, string cookieName, string cookieValue)
        {
            if (response.Cookies.AllKeys.Contains(cookieName))
            {
                response.Cookies[cookieName].Value = cookieValue;
                response.Cookies[cookieName].Expires = DateTime.UtcNow.AddDays(-1);
            }
        }
        #endregion

        protected void Application_Start()
        {
            AntiForgeryConfig.SuppressXFrameOptionsHeader = true;
            AreaRegistration.RegisterAllAreas();

            AuthConfig.RegisterAuth();
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs
            
            // Get the exception object.
            Exception exc = Server.GetLastError();

            // Handle HTTP errors
            if (exc.GetType() == typeof(HttpException) || exc.GetType() == typeof(System.Runtime.Serialization.SerializationException))
            {
                if (exc.Message.Contains("NoCatch") || exc.Message.Contains("maxUrlLength") || exc.Message.Contains("InMemoryTokenManager") || exc.Message.Contains("non-serializable") || exc.Message.Contains("The controller for path")) return;
            }

            AuditLogManager.ReportWebException(Server, AuditEventType.SystemErrorWeb);

            Server.ClearError();
            Response.RedirectToRoute(new { controller = "Home", action = "GeneralError" });
             
        }

        protected void Session_OnStart()
        {

            //if user has existing OCM session token, sign in automatically
            var sessionToken = GetCookie(new HttpRequestWrapper(Request), "OCMSessionToken");
            var identifier = GetCookie(new HttpRequestWrapper(Request), "Identifier");
            if (!String.IsNullOrEmpty(sessionToken) && !String.IsNullOrEmpty(identifier))
            {
                //got token, if valid sign in users
                var userManager = new OCM.API.Common.UserManager();
                var user = userManager.GetUserFromIdentifier(identifier,sessionToken);
                if (user != null)
                {
                    OCM.MVC.Controllers.LoginProviderController.PerformCoreLogin(user);
                }
           
            }
        }
    }
}