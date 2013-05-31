using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCM.API.Common.Model;
using Newtonsoft.Json.Linq;
using System.Xml;

namespace OCM.Import.Providers
{
    public class ImportProvider_GenericKML : BaseImportProvider, IImportProvider
    {
        public ImportProvider_GenericKML()
        {
            ProviderName = "Generic_KML";
            OutputNamePrefix = "kml";
            AutoRefreshURL = null;
            IsAutoRefreshed = false;
            IsProductionReady = true;
        }

        public virtual void SetDataProviderDetails(ChargePoint cp, XmlNode item)
        {
            throw new Exception("SetDataProviderDetails not implemented");
        }
        
        public virtual void ParseBasicDetails (ChargePoint cp, XmlNode item)
        {
            throw new Exception("ParseBasicDetails not implemented");
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
            this.ImportRefData = new CommonImportRefData(coreRefData);

            List<ChargePoint> outputList = new List<ChargePoint>();

            XmlDocument xmlDoc = new XmlDocument();
            InputData = InputData.Replace(" xmlns=\"http://earth.google.com/kml/2.0\"","");
            xmlDoc.LoadXml(InputData);

            XmlNodeList dataList = xmlDoc.SelectNodes("//Placemark");

            int itemCount = 0;
          
            foreach (XmlNode item in dataList)
            {
                bool skipItem = false;

                ChargePoint cp = new ChargePoint();
                cp.DateLastStatusUpdate = DateTime.Now;
                
                SetDataProviderDetails(cp, item);
                
                cp.AddressInfo = new AddressInfo();

                ParseBasicDetails(cp, item);

                //parse coordinates
                string[] posString = item["Point"]["coordinates"].InnerText.Split(',');
                cp.AddressInfo.Latitude = double.Parse(posString[1]);
                cp.AddressInfo.Longitude = double.Parse(posString[0]);

                //determine country or leave for geolocation
                int? countryID = null;
               
                if (countryID == null)
                {
                    this.Log("Country Not Matched, will require Geolocation:" + cp.AddressInfo.AddressLine1.ToString());
                }
                else
                {
                    cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(cy => cy.ID == countryID);
                }

                cp.Connections = ParseConnectionInfo(item);
                
                ParseAdditionalData(cp, item, coreRefData);

                cp.DataQualityLevel = 2; //lower than average quality due to spare data
                cp.SubmissionStatus = ImportRefData.SubmissionStatus_Imported;

                if (cp.StatusType == ImportRefData.Status_PlannedForFuture)
                {
                    skipItem = true;
                }

                if (!skipItem) outputList.Add(cp);

                itemCount++;
            }

            return outputList;
        }

        public virtual List<ConnectionInfo> ParseConnectionInfo(XmlNode item)
        {
            throw new Exception("ParseConnectionInfo not implemented");
        }

        public virtual void ParseAdditionalData(ChargePoint cp, XmlNode item, CoreReferenceData coreRefData)
        {
            throw new Exception("ParseAdditionalData not implemented");
        }
    }
}
