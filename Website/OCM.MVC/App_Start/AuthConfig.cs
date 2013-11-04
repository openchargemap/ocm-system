using Microsoft.Web.WebPages.OAuth;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace OCM.MVC
{
    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            //For more information visit http://go.microsoft.com/fwlink/?LinkID=252166

           
            OAuthWebSecurity.RegisterTwitterClient(
                consumerKey: ConfigurationManager.AppSettings["twitterConsumerKey"],
                consumerSecret: ConfigurationManager.AppSettings["twitterConsumerSecret"]);

            //OAuthWebSecurity.RegisterMicrosoftClient(
              // clientId: ConfigurationManager.AppSettings["microsoftAppId"],
               //clientSecret: ConfigurationManager.AppSettings["microsoftClientSecret"]);

            OAuthWebSecurity.RegisterFacebookClient(
                appId: ConfigurationManager.AppSettings["facebookAppId"],
                appSecret:  ConfigurationManager.AppSettings["facebookAppSecret"]);

            OAuthWebSecurity.RegisterGoogleClient();
        }
    }
}