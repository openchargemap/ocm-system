using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;

namespace OCM.Import.Providers
{
    public class ImportProvider_BlinkNetwork : BaseImportProvider, IImportProvider
    {
        public ImportProvider_BlinkNetwork()
        {
            ProviderName = "blinknetwork.com";
            OutputNamePrefix = "blink";
            AutoRefreshURL = "http://www.blinknetwork.com/locator/locations?lat=0&lng=0&latd=180&lngd=360&mode=avail&n=100000&admin=false";
            IsAutoRefreshed = true;
            IsProductionReady = true;
        }

        List<ChargePoint> IImportProvider.Process(CoreReferenceData coreRefData)
        {

            List<ChargePoint> outputList = new List<ChargePoint>();
            
            
            var submissionStatus = coreRefData.SubmissionStatusTypes.First(s => s.ID == 100);//imported and published
            var operationalStatus = coreRefData.StatusTypes.First(os => os.ID == 50);
            var operationalMixedStatus = coreRefData.StatusTypes.First(os => os.ID == 75);
            var unknownStatus = coreRefData.StatusTypes.First(os => os.ID == 0);
            var usageTypePublic = coreRefData.UsageTypes.First(u => u.ID == 1);
            var usageTypePrivate = coreRefData.UsageTypes.First(u => u.ID == 2);
          
            var networkOperator = coreRefData.Operators.First(op=>op.ID==9); //blink/ecotality

            string jsString = InputData;
            jsString = "{ \"data\": " + jsString + "}"; //fix data by wrapping on container
            
            JObject o = JObject.Parse(jsString);
            
            var response = o.Values();
            var data = response.Values();
            var dataList = data.Values().ToArray();
            int itemCount = 0;
            
            foreach (var item in data)
            {
                bool skipItem = false;
                try
                {
                    ChargePoint cp = new ChargePoint();
                    cp.AddressInfo = new AddressInfo();

                    cp.OperatorInfo = networkOperator;
                    cp.OperatorsReference = item["encid"].ToString();
                    cp.DataProvider = new DataProvider() { ID = 17 }; //blinknetwork.com
                    cp.DataProvidersReference = item["id"].ToString();
                    cp.DateLastStatusUpdate = DateTime.Now;

                    cp.AddressInfo.Title = item["name"] != null ? item["name"].ToString() : item["name"].ToString();
                    cp.AddressInfo.RelatedURL = "http://www.blinknetwork.com";
                    cp.DateLastStatusUpdate = DateTime.Now;

                    cp.AddressInfo.Latitude = double.Parse(item["latitude"].ToString());
                    cp.AddressInfo.Longitude = double.Parse(item["longitude"].ToString());

                    cp.AddressInfo.AddressLine1 = item["address1"].ToString();
                    cp.AddressInfo.AddressLine2 = item["address2"].ToString();
                    cp.AddressInfo.Town = item["city"].ToString();
                    cp.AddressInfo.StateOrProvince = item["state"].ToString();
                    cp.AddressInfo.Postcode = item["zip"].ToString();

                    //set country property
                    cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(c => c.ISOCode == item["country"].ToString());

                    string usageTypeCode = item["type"].ToString();

                    switch (usageTypeCode) {
                        case "COMMERCIAL":  cp.UsageType = coreRefData.UsageTypes.FirstOrDefault(u => u.ID == 5); //pay at location
                            break;
                        case "RESIDENTIAL": skipItem=true;
                            break;
                        default: 
                            Log("Unmatched usage type:"+usageTypeCode);
                            break;
                    }

                    cp.NumberOfPoints = int.Parse(item["chargers"].ToString());
                    int numOffline = int.Parse(item["offline"].ToString());
                    if (numOffline > 0)
                    {
                        cp.StatusType = operationalMixedStatus;
                    }
                    else
                    {
                        cp.StatusType = operationalStatus;
                    }

                    //populate connections
                    cp.Connections = new List<ConnectionInfo>();
                    var levelTypes = item["levels"].ToArray();
                    foreach (var level in levelTypes)
                    {
                        ConnectionInfo con = new ConnectionInfo();
                        if (level.ToString() == "1")
                        {
                            con.ConnectionType = new ConnectionType { ID = 1 };//J1772
                            con.Level = new ChargerType { ID = 1 };
                        }
                        if (level.ToString() == "2")
                        {
                            con.ConnectionType = new ConnectionType { ID = 1 };//J1772
                            con.Voltage = 220;
                            con.Level = new ChargerType { ID = 2 };
                        }
                        if (level.ToString() == "3")
                        {
                            con.ConnectionType = new ConnectionType { ID = 3 };//J1772
                            con.Voltage = 480;
                            con.Level = new ChargerType { ID = 3 };
                        }
                        cp.Connections.Add(con);
                    }
                    cp.DataQualityLevel = 3; //avg, higher than default

                    cp.SubmissionStatus = submissionStatus;
                    if (!skipItem) outputList.Add(cp);
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
