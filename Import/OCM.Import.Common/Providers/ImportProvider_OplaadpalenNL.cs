using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCM.API.Common.Model;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;
using System.Configuration;

namespace OCM.Import.Providers
{
    public class ImportProvider_OplaadpalenNL : BaseImportProvider, IImportProvider
    {
        
        public ImportProvider_OplaadpalenNL()
        {
            AutoRefreshURL = "http://oplaadpalen.nl/api/chargingpoints/" + ConfigurationManager.AppSettings["ImportProviderAPIKey_OplaadpalenNL"].ToString() + "/json?vehicletype=car";

            ProviderName = "Oplaadpalen";
            OutputNamePrefix = "OplaadpalenNL_";
            
            IsAutoRefreshed = true;
            IsProductionReady = true;

            SourceEncoding = Encoding.GetEncoding("UTF-8");

            DataProviderID = 26; //Oplaadpalen.nl
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {

            //TODO: operator not well matched, usage type not known, multiple connectors at same site not imported due to duplicate POI. Requires merge process.
            List<ChargePoint> outputList = new List<ChargePoint>();

            var submissionStatus = coreRefData.SubmissionStatusTypes.First(s => s.ID == 100);//imported and published
            var operationalStatus = coreRefData.StatusTypes.First(os => os.ID == 50);
            var unknownStatus = coreRefData.StatusTypes.First(os => os.ID == 0);
            var usageTypePublic = coreRefData.UsageTypes.First(u => u.ID == 1);
            var usageTypePrivate = coreRefData.UsageTypes.First(u => u.ID == 2);
            var usageTypePrivateForStaffAndVisitors = coreRefData.UsageTypes.First(u => u.ID == 6); //staff and visitors
            var operatorUnknown = coreRefData.Operators.First(opUnknown => opUnknown.ID == 1);

            int itemCount = 0;

            string jsonString = "{ \"data\": " + InputData + "}"; 

            JObject o = JObject.Parse(jsonString);
            var dataList = o.Values().First().ToArray();

            var distinctCountries = new List<string>();
            foreach (var item in dataList)
            {
                /*
                 * {
  "id": "5813",
  "lng": "5.14287",
  "lat": "52.07858",
  "name": "NewMotion NL-TNM-FC11",
  "address": "Herculesplein 300",
  "postalcode": "3584 AA",
  "city": "Utrecht",
  "country": "NL",
  "phone": "",
  "url": "",
  "owner": "BP",
  "email": "",
  "opentimes": "ma-vr:6:00-23.30 za-zo:8:00-23.30 ",
  "chargetype": "DC snellader",
  "connectortype": "chademo",
  "nroutlets": "2",
  "cards": [
    "contant"
  ],
  "pricemethod": "per laadbeurt",
  "price": "6.00",
  "power": "50kW",
  "vehicle": "auto",
  "facilities": [
    "wifi",
    "wc",
    "parkeer",
    "restaurant",
    "wachtruimte",
    "koffiecorner",
    "shop",
    "openbaar vervoer"
  ],
  "realtimestatus": false
}*/
                ChargePoint cp = new ChargePoint();
                cp.DataProvider = new DataProvider() { ID = this.DataProviderID }; //AddEnergie
                cp.DataProvidersReference = item["id"].ToString();
                cp.DateLastStatusUpdate = DateTime.UtcNow;

                cp.AddressInfo = new AddressInfo();

                cp.AddressInfo.Title = item["address"].ToString();
                cp.OperatorsReference = item["name"].ToString();

                cp.AddressInfo.AddressLine1 = item["address"].ToString().Trim();
                cp.AddressInfo.Town = item["city"].ToString().Trim();
                //cp.AddressInfo.StateOrProvince = item["StateOrProvince"].ToString().Trim();
                cp.AddressInfo.Postcode = item["postalcode"].ToString().Trim();
                cp.AddressInfo.Latitude = double.Parse(item["lat"].ToString());
                cp.AddressInfo.Longitude = double.Parse(item["lng"].ToString());

                var countryCode = item["country"].ToString().ToLower();

                if (!distinctCountries.Exists(c=>c==countryCode)) distinctCountries.Add(countryCode);

                //fix incorrect country codes
                if (countryCode == "au") countryCode = "at"; //austria, not australia
                if (countryCode == "ml") countryCode = "mt"; //malta, not mali
                if (countryCode == "tu") countryCode = "tr"; //turkey
                if (countryCode == "ad") countryCode = "";// leave for geocoding, probably not andorra
                if (countryCode == "sv") countryCode = "si"; //slovenia, not el salvador
                if (countryCode == "ir") countryCode = "ie"; //ireland, not iran

                cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(c => c.ISOCode.ToLower() == countryCode);
                if (!String.IsNullOrEmpty(item["url"].ToString())) cp.AddressInfo.RelatedURL = item["url"].ToString();
                if (!String.IsNullOrEmpty(item["email"].ToString())) cp.AddressInfo.ContactEmail = item["email"].ToString();
                if (!String.IsNullOrEmpty(item["phone"].ToString())) cp.AddressInfo.ContactTelephone1 = item["phone"].ToString();

                var price = item["price"].ToString();
                var pricemethod = item["pricemethod"].ToString();
                
                cp.UsageCost = (!String.IsNullOrEmpty(price)?price+" ":"") + pricemethod;
                //set network operators
                //cp.OperatorInfo = new OperatorInfo { ID = 89 };
                
                //TODO: Operator, usage,price, power, connector type
                var owner = item["owner"].ToString().ToLower();
                var operatoInfo = coreRefData.Operators.FirstOrDefault(op=>op.Title.ToLower().Contains(owner));

                if (operatoInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine("Unknown operator: "+owner);
                }
                else
                {
                    cp.OperatorID = operatoInfo.ID;
                }
                /*bool isPublic = bool.Parse(item["IsPublic"].ToString());
                if (isPublic)
                {
                    cp.UsageType = usageTypePublic;
                }
                else
                {
                    cp.UsageType = usageTypePrivate;
                }
                */

                cp.NumberOfPoints = int.Parse(item["nroutlets"].ToString());
                cp.StatusType = operationalStatus;

                //populate connectioninfo from Ports
                var connectorType = item["connectortype"].ToString();
                var chargetype = item["chargetype"].ToString();
                var power = item["power"].ToString();
                ConnectionInfo cinfo = new ConnectionInfo();

                try
                {
                    if (!String.IsNullOrEmpty(power))
                    {
                        cinfo.PowerKW = double.Parse(power.Replace("kW", ""));
                    }
                }
                catch (System.FormatException) { }

                if (connectorType.ToLower().Contains("j1772"))
                {
                    cinfo.ConnectionTypeID = (int)StandardConnectionTypes.J1772;
                    cinfo.LevelID = 2;
                } else  if (connectorType.ToLower().Contains("mennekes"))
                {
                    cinfo.ConnectionTypeID = (int)StandardConnectionTypes.MennekesType2;
                    cinfo.LevelID = 2;
                } else if (connectorType.ToLower().Contains("chademo"))
                {
                    cinfo.ConnectionTypeID = (int)StandardConnectionTypes.CHAdeMO;
                    cinfo.LevelID = 3;
                }
                else if (connectorType.ToLower().Contains("schuko"))
                {
                    cinfo.ConnectionTypeID = (int)StandardConnectionTypes.Schuko;
                    cinfo.LevelID = 2;
                }
                else {
                    System.Diagnostics.Debug.WriteLine("Unknown connectorType:" + connectorType);
                }

                if (cinfo.PowerKW >= 50)
                {
                    cinfo.LevelID = 3;
                }
                if (!String.IsNullOrEmpty(chargetype))
                {

                    if (chargetype.StartsWith("DC")) cinfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                    if (chargetype.StartsWith("AC simpel")) cinfo.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                    //TODO: 3 phase?
                    
                }
                
               // System.Diagnostics.Debug.WriteLine("Unknown chargetype:" + chargetype+ " "+power);
                
                if (cp.Connections == null)
                {
                    cp.Connections = new List<ConnectionInfo>();
                    if (!IsConnectionInfoBlank(cinfo))
                    {
                        cp.Connections.Add(cinfo);
                    }
                }
                
                if (cp.DataQualityLevel == null) cp.DataQualityLevel = 3;

                cp.SubmissionStatus = submissionStatus;

                outputList.Add(cp);
                itemCount++;
            }

            string temp = "";
            foreach (var countryCode in distinctCountries)
            {
                temp += ", " + countryCode;
            }
            System.Diagnostics.Debug.WriteLine(temp);

            return outputList;
        }
    }
}
