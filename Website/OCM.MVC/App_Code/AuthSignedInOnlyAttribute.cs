using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web.Mvc;


namespace OCM.MVC
{

    /// <summary>
    /// Custom authorization attribute [AuthSignedInOnly] to set specific controller actions to be Access Denied if user not signed in
    /// </summary>
    public class AuthSignedInOnlyAttribute : System.Web.Mvc.AuthorizeAttribute
    {

        protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            if (httpContext.Session == null)
            {
                return false;
            }

            if (Roles.Contains("Admin"))
            {
                if (httpContext.Session["UserID"] != null && httpContext.Session["IsAdministrator"] != null && (bool)httpContext.Session["IsAdministrator"] == true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (httpContext.Session["UserID"] == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectToRouteResult(new
                    System.Web.Routing.RouteValueDictionary(new { controller = "Home", action = "NotSignedIn" })
                    );
            }
            else
            {
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }

}