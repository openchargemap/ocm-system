using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OCM.API.Common;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OCM.API.Common.Model;

namespace OCM.API.InputProviders
{
    public class JSONInputProvider : InputProviderBase, IInputProvider
    {
        public bool ProcessEquipmentSubmission(HttpContext context, ref OCM.API.Common.Model.ChargePoint cp)
        {
            System.IO.StreamReader sr = new System.IO.StreamReader(context.Request.InputStream);
            //TODO: handle encoding (UTF etc) correctly
            string responseContent = sr.ReadToEnd().Trim();

            string jsonString = responseContent;

            try
            {
                JObject o = JObject.Parse(jsonString);

                JsonSerializer serializer = new JsonSerializer();
                cp = (Common.Model.ChargePoint)serializer.Deserialize(new JTokenReader(o), typeof(Common.Model.ChargePoint));

                //validate cp submission

                if (POIManager.IsValid(cp))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp);

                //submission failed
                return false;
            }
        }


        public bool ProcessUserCommentSubmission(HttpContext context, ref Common.Model.UserComment comment)
        {
            System.IO.StreamReader sr = new System.IO.StreamReader(context.Request.InputStream);
            //TODO: handle encoding (UTF etc) correctly
            string responseContent = sr.ReadToEnd().Trim();

            string jsonString = responseContent;

            try
            {
                JObject o = JObject.Parse(jsonString);

                JsonSerializer serializer = new JsonSerializer();
                comment = (Common.Model.UserComment)serializer.Deserialize(new JTokenReader(o), typeof(Common.Model.UserComment));

                return true;
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp);

                //submission failed
                return false;
            }
        }

        public bool ProcessContactUsSubmission(HttpContext context, ref ContactSubmission contactSubmission)
        {
            System.IO.StreamReader sr = new System.IO.StreamReader(context.Request.InputStream);
           
            string responseContent = sr.ReadToEnd().Trim();

            string jsonString = responseContent;

            try
            {
                JObject o = JObject.Parse(jsonString);

                JsonSerializer serializer = new JsonSerializer();
                contactSubmission = (ContactSubmission)serializer.Deserialize(new JTokenReader(o), typeof(ContactSubmission));

                return true;
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp);

                //submission failed
                return false;
            }
        }
    }
}