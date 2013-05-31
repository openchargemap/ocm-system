using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OCM.API.Common.Model;
using OCM.API.Common;

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

        public User GetUserFromAPICall(HttpContext context)
        {
            string Identifier = context.Request["Identifier"];
            string SessionToken = context.Request["SessionToken"];

            if (String.IsNullOrEmpty(Identifier) || String.IsNullOrEmpty(SessionToken))
            {
                return null;
            }
            else
            {
                return new UserManager().GetUserFromIdentifier(Identifier, SessionToken);
            }
        }
    }
}