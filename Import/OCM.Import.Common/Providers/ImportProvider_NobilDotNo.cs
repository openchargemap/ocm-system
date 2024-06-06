using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace OCM.Import.Providers
{
    public class ImportProvider_NobilDotNo : BaseImportProvider, IImportProvider
    {
        public ImportProvider_NobilDotNo(string apiKey)
        {
            ProviderName = "nobil.no";
            OutputNamePrefix = "nobildotno";
            ApiKey = apiKey;
            AutoRefreshURL = $"https://www.nobil.no/api/server/datadump.php?apikey={apiKey}&format=xml";
            IsAutoRefreshed = true;
            IsProductionReady = true;
            SourceEncoding = Encoding.GetEncoding("UTF-8");
            DataProviderID = 19;//nobil.no
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
            List<ChargePoint> outputList = new List<ChargePoint>();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(InputData);

            XmlNodeList dataList = xmlDoc.SelectNodes("//chargerstation");

            var submissionStatus = coreRefData.SubmissionStatusTypes.First(s => s.ID == 100);//imported and published
            var operationalStatus = coreRefData.StatusTypes.First(os => os.ID == 50);
            var unknownStatus = coreRefData.StatusTypes.First(os => os.ID == 0);
            var usageTypePublic = coreRefData.UsageTypes.First(u => u.ID == 1);
            var usageTypePrivate = coreRefData.UsageTypes.First(u => u.ID == 2);
            var usageTypePrivateForStaffAndVisitors = coreRefData.UsageTypes.First(u => u.ID == 6); //staff and visitors
            var operatorUnknown = coreRefData.Operators.First(opUnknown => opUnknown.ID == 1);

            int itemCount = 0;
            foreach (XmlNode chargerstation in dataList)
            {
                var item = chargerstation.SelectNodes("metadata").Item(0);
                ChargePoint cp = new ChargePoint();
                cp.DataProviderID = this.DataProviderID; //nobil.no
                cp.DataProvidersReference = item["id"].InnerText; //is id unique across countries?
                cp.DateLastStatusUpdate = DateTime.UtcNow;
                cp.AddressInfo = new AddressInfo();

                cp.AddressInfo.Title = item["name"].InnerText != null ? item["name"].InnerText : item["Street"].InnerText;
                cp.AddressInfo.Title = cp.AddressInfo.Title.Trim().Replace("&amp;", "&");
                //cp.AddressInfo.RelatedURL = item["url"].ToString();

                cp.DateLastStatusUpdate = DateTime.UtcNow;
                cp.AddressInfo.AddressLine1 = item["Street"].InnerText;
                if (item["House_number"] != null) cp.AddressInfo.AddressLine1 += " " + item["House_number"].InnerText;
                cp.AddressInfo.Town = item["City"].InnerText.Trim();
                cp.AddressInfo.StateOrProvince = item["County"].InnerText.Trim();
                cp.AddressInfo.Postcode = item["Zipcode"].InnerText.Trim();
                string posString = item["Position"].InnerText.Trim();

                int sepPos = posString.IndexOf(",") - 1;
                string lat = posString.Substring(1, sepPos);
                sepPos += 2;
                string lon = posString.Substring(sepPos, (posString.Length - sepPos) - 1);
                cp.AddressInfo.Latitude = double.Parse(lat);
                cp.AddressInfo.Longitude = double.Parse(lon);

                //default to norway
                var countryCode = item["Land_code"].InnerText;
                if (countryCode.ToUpper() == "NOR")
                {
                    cp.AddressInfo.CountryID = 168;
                }
                else if (countryCode.ToUpper() == "FIN")
                {
                    cp.AddressInfo.CountryID = 79;
                }
                else if (countryCode.ToUpper() == "SWE")
                {
                    cp.AddressInfo.CountryID = 216;
                }
                else if (countryCode.ToUpper() == "DAN")
                {
                    cp.AddressInfo.CountryID = 65;
                }
                else if (countryCode.ToUpper() == "ISL")
                {
                    cp.AddressInfo.CountryID = 105;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Unknown country code:" + countryCode);
                }

                cp.AddressInfo.AccessComments = item["Description_of_location"].InnerText;
                cp.AddressInfo.GeneralComments = item["Contact_info"].InnerText;

                cp.NumberOfPoints = int.Parse(item["Number_charging_points"].InnerText);

                var attributes = chargerstation.SelectNodes("attributes").Item(0);
                var connectors = attributes.SelectSingleNode("connectors").SelectNodes("connector");
                foreach (XmlNode connector in connectors)
                {
                    var connectorAttribs = connector.SelectSingleNode("attribute[attrtypeid=4]");
                    var chargingCapacityAttribs = connector.SelectSingleNode("attribute[attrtypeid=5]");
                    var chargingModeAttribs = connector.SelectSingleNode("attribute[attrtypeid=20]");
                    ConnectionInfo cinfo = new ConnectionInfo() { };

                    cinfo.Reference = connector.Attributes["id"].InnerText;

                    if (connectorAttribs != null)
                    {
                        var connectorTypeVal = connectorAttribs.SelectSingleNode("attrvalid").InnerText;
                        if (connectorTypeVal == "14")
                        {
                            cinfo.ConnectionTypeID = 28;// Schuko CEE 7/4
                        }
                        else if (connectorTypeVal == "40")
                        {
                            cinfo.ConnectionTypeID = 27;// tesla supercharger connnector
                        }
                        else if (connectorTypeVal == "31")
                        {
                            //type 1 == J1772?
                            cinfo.ConnectionTypeID = (int)StandardConnectionTypes.J1772;
                        }
                        else if (connectorTypeVal == "29")
                        {
                            cinfo.ConnectionTypeID = 8;// tesla roadster
                        }
                        else if (connectorTypeVal == "32")
                        {
                            cinfo.ConnectionTypeID = 25;// type 2 (mennekes)
                        }
                        else if (connectorTypeVal == "50")
                        {
                            // Combination Connector - Type 2 + Schuko.
                            // Treat it as a Schuko and add a Type 2 connector.
                            cinfo.ConnectionTypeID = 28;
                            // Add a separate Type 2 connector.
                            ConnectionInfo cinfoType2 = new ConnectionInfo() { };

                            cinfoType2.Amps = 16;
                            cinfoType2.Voltage = 230;
                            cinfoType2.CurrentType = new CurrentType { ID = (int)StandardCurrentTypes.SinglePhaseAC };
                            cinfoType2.PowerKW = ((double)cinfoType2.Voltage * (double)cinfoType2.Amps) / 1000;

                            cinfoType2.Level = new ChargerType() { ID = 2 };
                            cinfoType2.ConnectionTypeID = 25;
                            if (cp.Connections == null)
                            {
                                cp.Connections = new List<ConnectionInfo>();
                            }
                            cp.Connections.Add(cinfoType2);
                        }
                        else if (connectorTypeVal == "30")
                        {
                            cinfo.ConnectionTypeID = (int)StandardConnectionTypes.CHAdeMO;
                        }
                        else if (connectorTypeVal == "34")
                        {
                            cinfo.ConnectionTypeID = 34;//IEC 60309 3 pin
                        }
                        else if (connectorTypeVal == "36")
                        {
                            cinfo.ConnectionTypeID = 35;//IEC 60309 5 pin
                        }
                        else if (connectorTypeVal == "39")
                        {
                            cinfo.ConnectionTypeID = 33;//Type 2 of CCS coupler
                        }
                        else if (connectorTypeVal == "41")
                        {
                            cinfo.ConnectionTypeID = (int)StandardConnectionTypes.CHAdeMO;//CCS combo + Chademo both present
                        }
                        else if (connectorTypeVal == "43")
                        {
                            cinfo.ConnectionTypeID = (int)StandardConnectionTypes.CHAdeMO;//CHAdeMO + Combo + AC-Type2 all present
                        }
                        else if (connectorTypeVal == "0")
                        {
                            cinfo.ConnectionTypeID = 0;//unknown
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Unnknown connectorDetails: " + connectorAttribs.InnerText);
                        }
                    }

                    if (chargingCapacityAttribs != null)
                    {
                        //TODO: 3-Phase power calcs are wrong.
                        var connectorTypeVal = chargingCapacityAttribs.SelectSingleNode("attrvalid").InnerText;
                        if (connectorTypeVal == "7")
                        {
                            cinfo.Amps = 16;
                            cinfo.Voltage = 230;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                            cinfo.LevelID = 2; //default to lvl2
                        }
                        else if (connectorTypeVal == "8")
                        {
                            cinfo.Amps = 32;
                            cinfo.Voltage = 230;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                            cinfo.LevelID = 2; //default to lvl2
                        }
                        else if (connectorTypeVal == "10")
                        {
                            cinfo.Amps = 16;
                            cinfo.Voltage = 400;
                            cinfo.PowerKW = 11;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.ThreePhaseAC;
                            cinfo.LevelID = 2; //default to lvl2
                        }
                        else if (connectorTypeVal == "11")
                        {
                            //500V DC
                            cinfo.Amps = 32;
                            cinfo.Voltage = 400;
                            cinfo.PowerKW = 22;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.ThreePhaseAC;
                            cinfo.LevelID = 3;
                        }
                        else if (connectorTypeVal == "12")
                        {
                            //400V AC (3 Phase) 63A
                            cinfo.Amps = 63;
                            cinfo.Voltage = 400;
                            cinfo.PowerKW = 43;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.ThreePhaseAC;
                            cinfo.LevelID = 3;
                        }
                        else if (connectorTypeVal == "13")
                        {
                            //tesla super charger
                            cinfo.Amps = 100;
                            cinfo.Voltage = 500;
                            cinfo.PowerKW = 50;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                            cinfo.LevelID = 3;
                        }
                        else if (connectorTypeVal == "16")
                        {
                            cinfo.Amps = 16;
                            cinfo.Voltage = 230;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.ThreePhaseAC;
                            cinfo.LevelID = 2; //default to lvl2
                        }
                        else if (connectorTypeVal == "17")
                        {
                            cinfo.Amps = 32;
                            cinfo.Voltage = 230;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.ThreePhaseAC;
                            cinfo.LevelID = 2; //default to lvl2
                        }
                        else if (connectorTypeVal == "19")
                        {
                            //500V DC MAX 50A
                            cinfo.Amps = 50;
                            cinfo.Voltage = 500;
                            cinfo.PowerKW = 20;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                            cinfo.LevelID = 3;
                        }
                        else if (connectorTypeVal == "20")
                        {
                            //TODO: 500VDC max 200A + 400V 3-phase max 63A
                            cinfo.Amps = 200;
                            cinfo.Voltage = 500;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                            cinfo.LevelID = 3;
                        }
                        else if (connectorTypeVal == "22")
                        {
                            //480VDC max 270A
                            cinfo.Amps = 270;
                            cinfo.Voltage = 480;
                            cinfo.PowerKW = 135;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                            cinfo.LevelID = 3;
                        }
                        else if (connectorTypeVal == "23")
                        {
                            //100 kW - 500VDC max 200A
                            cinfo.Amps = 200;
                            cinfo.Voltage = 500;
                            cinfo.PowerKW = 100;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                            cinfo.LevelID = 3;
                        }
                        else if (connectorTypeVal == "24")
                        {
                            //150 kW - 400VDC max 375A
                            cinfo.Amps = 375;
                            cinfo.Voltage = 400;
                            cinfo.PowerKW = 150;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                            cinfo.LevelID = 3;
                        }
                        else if (connectorTypeVal == "25")
                        {
                            //350 kW 
                            cinfo.PowerKW = 350;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                            cinfo.LevelID = 3;
                        }
                        else if (connectorTypeVal == "27")
                        {
                            //Tesla Supercharger - Up to 120kW. Will be upgraded to 130kW.
                            cinfo.Amps = 200;
                            cinfo.Voltage = 500;
                            cinfo.PowerKW = 120;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                            cinfo.LevelID = 3;
                        }
                        else if (connectorTypeVal == "28")
                        {

                            cinfo.Amps = 125;
                            cinfo.Voltage = 400;
                            cinfo.PowerKW = 50;
                            cinfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                            cinfo.LevelID = 3;
                        }
                        else if (connectorTypeVal == "0")
                        {
                            //unknown power level
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("unknown chargingCapacity: " + chargingCapacityAttribs.InnerText);
                        }
                    }

                    // Only calculate power if power is not explicitly set.
                    // TODO : standardise across data import providers
                    if (cinfo.PowerKW == null)
                    {
                        cinfo.PowerKW = (double?)ComputePowerkWForConnectionInfo(cinfo);
                    }

                    if (chargingModeAttribs != null)
                    {
                        var chargeMode = chargingModeAttribs.SelectSingleNode("trans");
                        if (chargeMode != null)
                        {
                            cinfo.Comments = chargeMode.InnerText;
                        }
                    }
                    if (cp.Connections == null)
                    {
                        cp.Connections = new List<ConnectionInfo>();
                    }

                    if (!IsConnectionInfoBlank(cinfo))
                    {
                        cp.Connections.Add(cinfo);
                    }
                }

                if (cp.DataQualityLevel == null) cp.DataQualityLevel = 2;

                cp.SubmissionStatus = submissionStatus;

                outputList.Add(cp);
                itemCount++;
            }

            return outputList;
        }
    }
}
