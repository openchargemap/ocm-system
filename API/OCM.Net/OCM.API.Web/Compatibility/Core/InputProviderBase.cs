using Microsoft.AspNetCore.Http;
using OCM.API.Common;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.InputProviders
{
    public class InputProviderBase
    {
        public double? ParseDouble(string val)
        {
            double result = 0;

            if (double.TryParse(val, out result))
                return result;
            else
                return null;
        }

        /// <summary>
        /// Parse string, limiting to 100 characters
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public string ParseString(string val)
        {
            if (val != null)
            {
                //limit string parameters to 100 characters
                if (val.Length > 100) val = val.Substring(0, 100);
            }
            return val;
        }

        /// <summary>
        /// Parse string, limiting to 1000 characters
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public string ParseLongString(string val)
        {
            if (val != null)
            {
                //limit string parameters to 1000 characters
                if (val.Length > 1000) val = val.Substring(0, 1000);
            }
            return val;
        }

        public int? ParseInt(string val)
        {
            int result = 0;

            if (int.TryParse(val, out result))
                return result;
            else
                return null;
        }

        public User GetUserFromAPICall(HttpContext context, string apiKey)
        {


            if (!string.IsNullOrEmpty(apiKey))
            {
                var user =  new UserManager().GetUserFromAPIKey(apiKey);
                return user;
            }

            //TODO: move to security

            string JWTAuthToken = null;

            //attempt to read JWT Auth from request headers, or fetch from body of request
            IEnumerable<string> authHeaderValues = context.Request.Headers.GetCommaSeparatedValues("Authorization");

            if (authHeaderValues == null || !authHeaderValues.Any())
            {
                //not in header, attempt to get from request body
                JWTAuthToken = context.Request.Query["jwt-bearer"];
            }
            else
            {
                //got token in header
                var bearerToken = authHeaderValues.ElementAt(0);
                JWTAuthToken = bearerToken.StartsWith("Bearer ") ? bearerToken.Substring(7) : bearerToken;
            }

            if (JWTAuthToken == null)
            {
                string Identifier = context.Request.Query["Identifier"];
                string SessionToken = context.Request.Query["SessionToken"];

                //legacy identifier + session token method
                if (String.IsNullOrEmpty(Identifier) || String.IsNullOrEmpty(SessionToken))
                {
                    return null;
                }
                else
                {
                    return new UserManager().GetUserFromIdentifier(Identifier, SessionToken);
                }
            }
            else
            {
                //validate token and return matching user profile
                var submittedToken = OCM.API.Security.JWTAuth.ParseEncodedJWT(JWTAuthToken);
                var userIdClaim = submittedToken.Claims.FirstOrDefault(c => c.Type == "UserID");
                if (userIdClaim != null)
                {
                    var userProfile = new UserManager().GetUser(int.Parse(userIdClaim.Value));

                    if (userProfile != null)
                    {
                        var claims = OCM.API.Security.JWTAuth.ValidateJWTForUser(JWTAuthToken, userProfile);
                        if (claims != null)
                        {
                            if (claims.HasClaim(c => c.Type == "nonce" && c.Value == userProfile.CurrentSessionToken))
                            {
                                return userProfile;
                            }
                        }
                    }
                }
            }

            //could not resolve or validate user
            return null;
        }
    }
}