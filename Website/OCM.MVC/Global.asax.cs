using OCM.API.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

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
    }
}