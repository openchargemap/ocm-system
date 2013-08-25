using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using OCM.API.Common.Model;

namespace OCM.Import.Providers
{
    public class ImportProvider_eLaad : BaseImportProvider, IImportProvider
    {
        public ImportProvider_eLaad()
        {
            this.ProviderName = "e-laad.nl";
            OutputNamePrefix = "elaad";
            SourceEncoding = Encoding.GetEncoding("UTF-8");
        }

        private string GetAttributeValue(string attribName, string source)
        {
            source = source.Substring(source.IndexOf(">" + attribName) + attribName.Length + 5);
            int startPos = source.IndexOf("\"atr-value") + 12;
            int endPos = source.IndexOf("</span>");
            if (endPos >= startPos)
            {
                string val = source.Substring(startPos, endPos - startPos);
                return val;
            }
            else return "";
        }

        List<ChargePoint> IImportProvider.Process(CoreReferenceData coreRefData)
        {
            /*
            List<EVSE> outputList = new List<EVSE>();

            string source = InputData;
            StringReader s = new StringReader(source);
            int itemCount = 0;
            XmlTextReader reader = new XmlTextReader(s);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "content")
                {
                    try
                    {
                        EVSE evse = new EVSE();
                        string item = reader.ReadInnerXml();
                        item = item.Replace("&lt;", "<");
                        item = item.Replace("&gt;", ">");

                        evse.Title = GetAttributeValue("Address", item);
                        evse.Latitude = double.Parse(GetAttributeValue("CoordinateWgs84Latitude", item));
                        evse.Longitude = double.Parse(GetAttributeValue("CoordinateWgs84Longitude", item));
                        evse.Updated = DateTime.Now;
                        evse.ID = GetAttributeValue("Uid", item);
                        evse.ExtendedAttributes = new List<ExtendedAttribute>();
                        evse.ExtendedAttributes.Add(new ExtendedAttribute { Name = "City", Value = GetAttributeValue("City", item) });
                        evse.ExtendedAttributes.Add(new ExtendedAttribute { Name = "Country", Value = GetAttributeValue("Country", item) });
                        evse.ExtendedAttributes.Add(new ExtendedAttribute { Name = "PostalCode", Value = GetAttributeValue("PostalCode", item) });
                        //evse.ExtendedAttributes.Add(new ExtendedAttribute { Name = "Notes", Value = GetAttributeValue("Notes", item) });

                        int statusID = int.Parse(GetAttributeValue("Status", item));
                        switch (statusID)
                        {
                            case 0: evse.Status = "Initial";
                                    break;
                                case 1: evse.Status = "Planned";
                                    break;
                                case 2: evse.Status = "Available";
                                    break;
                                case 3: evse.Status = "Charging";
                                    break;
                                case 4: evse.Status = "Faulted";
                                    break;
                                 case 5: evse.Status = "Maintenance";
                                    break;
                                case 6: evse.Status = "Unavailable";
                                    break;
                                 case 7: evse.Status = "Offline";
                                    break;
                        }

                        outputList.Add(evse);
                       
                    }
                    catch (Exception)
                    {
                        Log("Error parsing item " + itemCount);
                    }
                    itemCount++;
                }
            }


            return outputList;
             * */
            return null;
        }
    }
}
