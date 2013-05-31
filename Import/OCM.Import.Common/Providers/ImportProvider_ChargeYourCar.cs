using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;

namespace OCM.Import.Providers
{
    public class ImportProvider_ChargeYourCar : BaseImportProvider, IImportProvider
    {
        public ImportProvider_ChargeYourCar()
        {
            ProviderName = "chargeyourcar.org.uk";
            OutputNamePrefix = "chargeyourcar";
        }

        List<ChargePoint> IImportProvider.Process(CoreReferenceData coreRefData)
        {
            /*
            List<EVSE> outputList = new List<EVSE>();

            string source = InputData;

            int startPos = source.IndexOf("addwithicon(\"");
            int endPos = source.LastIndexOf("<script type=\"text/javascript\">");

            string jsString = source.Substring(startPos, endPos - startPos);

            jsString = jsString.Replace("</script>","");
            jsString = jsString.Replace(" <script type=\"text/javascript\">","");
            
             jsString = jsString.Replace(");", "]},");

            
            jsString = jsString.Substring(0, jsString.LastIndexOf(","));
           // jsString = jsString.Substring(0, jsString.LastIndexOf(","));

            //jsString = jsString.Replace("\"", "|");
            //jsString = jsString.Replace("new google.maps.LatLng(", "\"");
            //jsString = jsString.Replace("),", "\",");
           // jsString = jsString.Replace("'", "\"");
           // jsString = jsString.Replace("|", "'");
            jsString = jsString.Replace("addwithicon(\"", "{\"value\" :[\"");
            while (jsString.Contains("href=\""))
            {
                string fragment = jsString.Substring(jsString.IndexOf("href=\"") + 6);
                fragment = fragment.Substring(0,fragment.IndexOf("\""));
                jsString= jsString.Replace("\"" + fragment + "\"", "'" + fragment + "'");
            }
            jsString = "{ \"data\":[ " + jsString + "]}";

            JObject o = JObject.Parse(jsString);
            var dataList = o.Values();

            int itemCount =0;
            foreach (var item in dataList.Values())
            {
                try
                {
                    EVSE evse = new EVSE();
                    evse.Link = "http://www.chargeyourcar.org.uk";
                    evse.Updated = DateTime.Now;
                    evse.ExtendedAttributes = new List<ExtendedAttribute>();
                      
                    JToken[]  elements = item["value"].Values().ToArray();
                    string content = elements[2].ToString();
                    int contentParsePos = content.IndexOf("<br>") + 4;

                    evse.Title = content.Substring(contentParsePos, content.IndexOf("</b>") - contentParsePos);
                    
                   
                    //evse.ID = item["position"].ToString().Replace(", ", "@");
                   
                   
                    //content = content.Substring(content.LastIndexOf("<p>") + 3, content.LastIndexOf("</p>") - (content.LastIndexOf("<p>") + 3));
                    evse.Content = content;

                    evse.Latitude = double.Parse(elements[0].ToString());
                    evse.Longitude = double.Parse(elements[1].ToString());

               
                    outputList.Add(evse);
                }
                catch (Exception)
                {
                    Log("Error parsing item "+itemCount);
                }

                itemCount++;
            }

            return outputList;
             * */
            return null;
        }
    }
}
