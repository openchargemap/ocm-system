using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace OCM.MVC
{
    /*
    public class IsRootActionConstraint : IRouteConstraint
    {
        private Dictionary<string, Type> _controllers;

        public IsRootActionConstraint()
        {
            _controllers = Assembly
                                .GetCallingAssembly()
                                .GetTypes()
                                .Where(type => type.IsSubclassOf(typeof(Controller)))
                                .ToDictionary(key => key.Name.Replace("Controller", ""));
        }

        #region IRouteConstraint Members

        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            string action = values["action"] as string;
            // Check for controller names
            return !_controllers.Keys.Contains(action);
        }

        #endregion
    }
    */

    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "DefaultWithLanguage",
                url: "{languagecode}/{controller}/{action}/{id}",
                defaults: new { languagecode="en", controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

           /* routes.MapRoute(
               name: "Browse",
               url: "browse/item/{id}",
               defaults: new { controller = "POI", action = "Index", id = UrlParameter.Optional }
           )*/
            
        }
    }
}