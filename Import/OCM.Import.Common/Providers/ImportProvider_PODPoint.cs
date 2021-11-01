using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.Import.Providers
{
    public class ImportProvider_PODPoint : BaseImportProvider, IImportProvider
    {
        public ImportProvider_PODPoint()
        {
            ProviderName = "pod-point.com";
            OutputNamePrefix = "pod-point";
        }

        public List<ChargePoint> Process(CoreReferenceData coreRefData)
        {

            List<ChargePoint> outputList = new List<ChargePoint>();

            string source = InputData;

            int startPos = source.IndexOf("*markers");
            int endPos = source.LastIndexOf("id=\"footer\"");
            DataProvider dataProvider = coreRefData.DataProviders.FirstOrDefault(d => d.ID == 16); //POD Point
            OperatorInfo operatorInfo = coreRefData.Operators.FirstOrDefault(op => op.ID == 3);

            string jsString = "/*" + source.Substring(startPos, endPos - startPos);

            jsString = jsString.Replace("makeMarker(", "");
            jsString = jsString.Replace(");", ",");

            jsString = jsString.Substring(0, jsString.LastIndexOf(","));
            jsString = jsString.Substring(0, jsString.LastIndexOf(","));

            jsString = jsString.Replace("/**markers", "");
            jsString = jsString.Replace("/**", "");
            jsString = jsString.Replace("*markers", "");

            jsString = jsString.Replace("**/", "");
            jsString = jsString.Replace("*/", "");
            jsString = jsString.Replace("\"", "|");
            jsString = jsString.Replace("new google.maps.LatLng(", "\"");
            jsString = jsString.Replace("),", "\",");
            jsString = jsString.Replace("'", "\"");
            jsString = jsString.Replace("|", "'");

            jsString = "{ \"data\":[ " + jsString + "]}";

            JObject o = JObject.Parse(jsString);
            var dataList = o.Values();

            int itemCount = 0;
            foreach (var item in dataList.Values())
            {
                try
                {
                    ChargePoint cp = new ChargePoint();
                    cp.DataProvider = dataProvider;
                    cp.OperatorInfo = operatorInfo;

                    cp.AddressInfo.Title = item["title"].ToString();
                    cp.AddressInfo.RelatedURL = "http://www.pod-point.com";
                    cp.DataProvidersReference = item["position"].ToString().Replace(", ", "@");
                    cp.DateLastStatusUpdate = DateTime.UtcNow;

                    string content = item["content"].ToString();
                    content = content.Substring(content.LastIndexOf("<p>") + 3, content.LastIndexOf("</p>") - (content.LastIndexOf("<p>") + 3));
                    cp.GeneralComments = content;
                    string[] pos = item["position"].ToString().Split(',');
                    cp.AddressInfo.Latitude = double.Parse(pos[0]);
                    cp.AddressInfo.Longitude = double.Parse(pos[1]);

                    string status = "";
                    string itemIcon = item["icon"].ToString();
                    if (itemIcon.EndsWith("mappodpointblue.png")) status = "Available";
                    if (itemIcon.EndsWith("mappodpointgreen.png")) status = "In Use";
                    if (itemIcon.EndsWith("mappodpointbw.png")) status = "Planned";
                    if (itemIcon.EndsWith("mappodpointred.png")) status = "Not Operational";
                    if (!String.IsNullOrEmpty(status))
                    {
                        var statusType = coreRefData.StatusTypes.FirstOrDefault(s => s.Title.ToLower() == status.ToLower());
                        if (statusType != null)
                        {
                            cp.StatusType = statusType;
                        }
                    }
                    outputList.Add(cp);
                }
                catch (Exception)
                {
                    Log("Error parsing item " + itemCount);
                }

                itemCount++;
            }

            return outputList;
        }
    }
}
