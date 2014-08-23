using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace OCM.MVC
{
 
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.LowercaseUrls = true;

            routes.MapMvcAttributeRoutes();
 
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