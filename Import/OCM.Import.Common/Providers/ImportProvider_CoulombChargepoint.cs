using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System.Web.Script.Serialization;
using System.Globalization;
using System.Threading;

namespace OCM.Import.Providers
{
    public class ImportProvider_CoulombChargepoint : BaseImportProvider, IImportProvider
    {
        public ImportProvider_CoulombChargepoint()
        {
            ProviderName = "chargepointportal.net";
            OutputNamePrefix = "Coulomb";
            //AutoRefreshURL = "https://webservices.chargepointportal.net:8081/coulomb_api_1.1.wsdl";
            IsAutoRefreshed = false;
            IsProductionReady = false;
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
          
            var networkOperator = coreRefData.Operators.First(op=>op.ID==5); //Coulomb Chargepoint Network

            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            TextInfo textInfo = cultureInfo.TextInfo;

            string jsString = "{ \"data\": " + InputData + "}";

            JavaScriptSerializer jss = new JavaScriptSerializer();
            jss.RegisterConverters(new JavaScriptConverter[] { new DynamicJsonConverter() });
            dynamic parsedList = jss.Deserialize(jsString, typeof(object)) as dynamic;
            var dataList = parsedList.data;

            int itemCount = 0;
            foreach (var item in dataList)
            {
                try
                {
                    ChargePoint cp = new ChargePoint();
                    cp.AddressInfo = new AddressInfo();

                    cp.OperatorInfo = networkOperator;
                    cp.OperatorsReference = item["StationID"].ToString();
                    cp.DataProvider = new DataProvider() { ID = 20 }; //couloumb
                    cp.DataProvidersReference = item["StationID"].ToString();
                    cp.DateLastStatusUpdate = DateTime.UtcNow;

                    cp.AddressInfo.Title = item["Name"] != null ? item["Name"].ToString() : item["StationID"].ToString();
                    cp.AddressInfo.Title = textInfo.ToTitleCase(cp.AddressInfo.Title.ToLower());
                    cp.AddressInfo.RelatedURL = "http://www.chargepoint.net";
                    cp.DateLastStatusUpdate = DateTime.UtcNow;

                    cp.AddressInfo.Latitude = double.Parse(item["Geo"]["lat"].ToString());
                    cp.AddressInfo.Longitude = double.Parse(item["Geo"]["long"].ToString());

                    cp.AddressInfo.AddressLine1 = item["Address"].ToString();
                    //cp.AddressInfo.AddressLine2 = item["address2"].ToString();
                    cp.AddressInfo.Town = item["City"].ToString();
                    cp.AddressInfo.StateOrProvince = item["State"].ToString();
                    cp.AddressInfo.Postcode = item["postalCode"].ToString();

                    //set country property
                    string countryRef = item["Country"].ToString();
                    cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(c => c.Title == countryRef);

                    /*string usageTypeCode = item["type"].ToString();
                    if (usageTypeCode == "COMMERCIAL")
                    {
                        cp.UsageType = coreRefData.UsageTypes.FirstOrDefault(u => u.ID == 5); //pay at location
                    }
                    else
                    {
                        Log("Unmatched usage type:"+usageTypeCode);
                    }
                    */
                    cp.NumberOfPoints = int.Parse(item["Num_port"].ToString());
                    cp.GeneralComments = item["Description"].ToString();
                    
                    /*int numOffline = int.Parse(item["offline"].ToString());
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
                     * */
                    cp.DataQualityLevel = 3; //avg, higher than default

                    cp.SubmissionStatus = submissionStatus;
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
