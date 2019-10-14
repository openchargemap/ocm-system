using System;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OCM.MVC
{

    /// <summary>
    /// Custom authorization attribute [AuthSignedInOnly] to set specific controller actions to be Access Denied if user not signed in
    /// </summary>
    public class AuthSignedInOnlyAttribute : AuthorizeAttribute
    {

        protected bool AuthorizeCore(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (httpContext.Session == null)
            {
                return false;
            }

            if (Roles.Contains("Admin"))
            {
                if (httpContext.Session.GetInt32("UserID") != null && httpContext.Session.GetString("IsAdministrator") != null && bool.Parse(httpContext.Session.GetString("IsAdministrator")) == true)
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
                if (httpContext.Session.GetInt32("UserID") == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

       /* protected void HandleUnauthorizedRequest(AuthorizationContext filterContext)
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
        }*/
    }

}