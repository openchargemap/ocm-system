using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using OCM.API.Common.Model;

namespace OCM.Import.Providers
{
    public class ImportProvider_ESB_eCars : ImportProvider_GenericKML
    {
        public ImportProvider_ESB_eCars()
        {
            ProviderName = "ESB_eCars";
            OutputNamePrefix = "ESB";
            AutoRefreshURL = "https://www.esb.ie/electric-cars/kml/all.kml";
            IsAutoRefreshed = true;
            IsProductionReady = true;
            DataProviderID = 23;//ESB
        }

        public override void SetDataProviderDetails(ChargePoint cp, XmlNode item)
        {
            //ESB eCars
            cp.DataProvider = new DataProvider() { ID = this.DataProviderID };

            //invent unique id by hashing location title
            cp.DataProvidersReference = CalculateMD5Hash(RemoveFormattingCharacters(item["name"].InnerText));
        }

        public override void ParseBasicDetails(ChargePoint cp, XmlNode item)
        {
            //parse address info
            string[] loctitle = RemoveFormattingCharacters(item["name"].InnerText).Replace(" - ","~").Replace(",","~").Split('~');

            if (loctitle.Length==1 && loctitle[0].Contains("-")) loctitle = RemoveFormattingCharacters(item["name"].InnerText).Replace("-", "~").Replace(",", "~").Split('~');
            cp.AddressInfo.Town = loctitle[0].Trim();

            if (loctitle[1].Contains(","))
            {
                string[] addressinfo = loctitle[1].Split(',');
                cp.AddressInfo.Title = addressinfo[0].Trim();
            }
            else
            {
                cp.AddressInfo.Title = loctitle[1].Trim();
            }

            if (loctitle.Length > 2)
            {
                cp.AddressInfo.AddressLine1 = loctitle[2].Trim();
                if (loctitle.Length > 4)
                {
                    cp.AddressInfo.StateOrProvince = loctitle[4].Trim();
                    cp.AddressInfo.AddressLine2 = loctitle[3].Trim();
                }
                else if (loctitle.Length > 3)
                {
                    cp.AddressInfo.StateOrProvince = loctitle[3].Trim();
                }
            }
          

            //parse description
            string descriptionText = item["description"].InnerText;
            cp.StatusType = ImportRefData.Status_Unknown;

            if (descriptionText.Contains("<p>Operational</p>"))
            {
                cp.StatusType = ImportRefData.Status_Operational;
            }

            if (descriptionText.Contains("<p>Undergoing engineering design</p>"))
            {
                cp.StatusType = ImportRefData.Status_PlannedForFuture;
            }

            if (descriptionText.Contains("<p>Out of Service</p>"))
            {
                cp.StatusType = ImportRefData.Status_NonOperational;
            }
            //cp.AddressInfo.AddressLine1 = ConvertUppercaseToTitleCase(RemoveFormattingCharacters(item["description"].InnerText));
        }

        public override List<ConnectionInfo> ParseConnectionInfo(XmlNode item)
        {
            string descriptionText = item["description"].InnerText;
            string styleText = item["styleUrl"].InnerText;

            var Connections = new List<ConnectionInfo>();

            if ((styleText.Contains("#Fast") || styleText.Contains("#DCAC")) && descriptionText.Contains("DC"))
            {
                ConnectionInfo cinfo = new ConnectionInfo() { };
                cinfo.Quantity = 1;
                cinfo.ConnectionType = ImportRefData.ConnectionType_CHADEMO;
                cinfo.Level = ImportRefData.ChrgLevel_3;
                cinfo.Voltage = 480;
                cinfo.Amps = 100;


                cinfo.StatusType = ImportRefData.Status_Unknown;

                if (descriptionText.Contains("Operational"))
                {
                    cinfo.StatusType = ImportRefData.Status_Operational;
                }

                if (!IsConnectionInfoBlank(cinfo)) Connections.Add(cinfo);
            }

            if (styleText.Contains("#Med") && descriptionText.Contains("Type 2 (2 places)"))
            {
                ConnectionInfo cinfo = new ConnectionInfo() { };
                cinfo.Quantity = 2;
                cinfo.ConnectionType = ImportRefData.ConnectionType_Type2Mennekes;
                cinfo.Level = ImportRefData.ChrgLevel_2;
                cinfo.Voltage = 230;
                cinfo.Amps = 32;

                cinfo.StatusType = ImportRefData.Status_Unknown;

                if (descriptionText.Contains("Operational"))
                {
                    cinfo.StatusType = ImportRefData.Status_Operational;
                }

                if (!IsConnectionInfoBlank(cinfo)) Connections.Add(cinfo);
            }

            return Connections;
        }

        public override void ParseAdditionalData(ChargePoint cp, XmlNode item, CoreReferenceData coreRefData)
        {
            string descriptionText = item["description"].InnerText;
            if (descriptionText.Contains("<p>24 hour access</p>")) cp.AddressInfo.AccessComments = "24 Hour Access";
            if (descriptionText.Contains("Business hours only")) cp.AddressInfo.AccessComments = "Business Hours Only";

            if (descriptionText.Contains("Please contact the host premises")) cp.UsageType = ImportRefData.UsageType_PublicNoticeRequired;

            //attempt country match for locations which commonly have geocoding issues
            switch (cp.AddressInfo.Town.Trim())
            {
                case "Cork":
                case "Dublin":
                case "Meath":
                case "Waterford":
                case "Wicklow":
                case "Clare":
                case "Galway":
                case "Kerry":
                case "Wexford":
                    cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(c=>c.ISOCode=="IE");
                    break;
                case "Antrim":
                case "Derry": 
                    cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(c => c.ISOCode == "GB");
                    break;
            }

        }
    }
}
