using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using OCM.API.Common.Model;

namespace OCM.Import.Providers
{
    public enum ChademoImportType
    {
        Western,
        Japan
    }

    public class ImportProvider_ChademoGroup : ImportProvider_GenericKML
    {
        public ChademoImportType ImportType { get; set; }

        public ImportProvider_ChademoGroup()
        {
            ProviderName = "chademo.com";
            OutputNamePrefix = "chademo_kml";
            AutoRefreshURL = null;
            IsAutoRefreshed = false;
            IsProductionReady = true;
            ImportType = ChademoImportType.Western;
            DataProviderID = 22;//chademo
        }

        public override void SetDataProviderDetails(ChargePoint cp, XmlNode item)
        {
            cp.DataProvider = new DataProvider() { ID = this.DataProviderID }; //CHAdeMO.com
            //invent unique id by hashing location title
            cp.DataProvidersReference = CalculateMD5Hash(RemoveFormattingCharacters(item["name"].InnerText));
        }

        public override void ParseBasicDetails(ChargePoint cp, XmlNode item)
        {
            cp.AddressInfo.Title = RemoveFormattingCharacters(item["name"].InnerText);
            string sourcetext = item["description"].InnerText;
            if (ImportType == ChademoImportType.Japan)
            {
                int indexOfTelephone=  sourcetext.IndexOf("[");
                cp.AddressInfo.AddressLine1 = sourcetext.Substring(0, indexOfTelephone).Trim();
                cp.AddressInfo.ContactTelephone1 = sourcetext.Substring(indexOfTelephone+5,sourcetext.IndexOf("]")-(indexOfTelephone+5));
            }
            else
            {
                cp.AddressInfo.AddressLine1 = ConvertUppercaseToTitleCase(RemoveFormattingCharacters(item["description"].InnerText));
            }
        }

        public override List<ConnectionInfo> ParseConnectionInfo(XmlNode item)
        {
            var Connections = new List<ConnectionInfo>();

            ConnectionInfo cinfo = new ConnectionInfo() { };

            cinfo.Quantity = 1;
            cinfo.ConnectionType = ImportRefData.ConnectionType_CHADEMO;
            cinfo.Level = ImportRefData.ChrgLevel_3;
            cinfo.Voltage = 480;
            cinfo.Amps = 100;

            if (!IsConnectionInfoBlank(cinfo))
            {
                Connections.Add(cinfo);
            }
            return Connections;
        }

        public override void ParseAdditionalData(ChargePoint cp, XmlNode item, CoreReferenceData coreRefData)
        {
        }
    }
}
