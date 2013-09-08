using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCM.API.Common.Model;
using Newtonsoft.Json.Linq;

namespace OCM.Import.Providers
{
    public class ImportProvider_UKChargePointRegistry : BaseImportProvider, IImportProvider
    {
        public ImportProvider_UKChargePointRegistry()
        {
            ProviderName = "chargepoints.dft.gov.uk";
            OutputNamePrefix = "ukchargepointregistry";
            AutoRefreshURL = "http://chargepoints.dft.gov.uk/api/retrieve/registry/format/json";
            IsAutoRefreshed = true;
            IsProductionReady = true;
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
            List<ChargePoint> outputList = new List<ChargePoint>();

            string source = InputData;

            JObject o = JObject.Parse(source);

            var dataList = o["ChargeDevice"].ToArray();

            var submissionStatus = coreRefData.SubmissionStatusTypes.First(s => s.ID == 100);//imported and published
            var operationalStatus = coreRefData.StatusTypes.First(os => os.ID == 50);
            var nonoperationalStatus = coreRefData.StatusTypes.First(os => os.ID == 100);
            var unknownStatus = coreRefData.StatusTypes.First(os => os.ID == 0);
            var usageTypePublic = coreRefData.UsageTypes.First(u => u.ID == 1);
            var usageTypePrivate = coreRefData.UsageTypes.First(u => u.ID == 2);
            var usageTypePublicMembershipRequired = coreRefData.UsageTypes.First(u => u.ID == 4);
            var operatorUnknown = coreRefData.Operators.First(opUnknown => opUnknown.ID == 1);

            int itemCount = 0;

            foreach (var dataItem in dataList)
            {
                var item = dataItem;
                ChargePoint cp = new ChargePoint();
                cp.DataProvider = new DataProvider() { ID = 18 }; //UK National Charge Point Registry
                cp.DataProvidersReference = item["ChargeDeviceId"].ToString();
                cp.DateLastStatusUpdate = DateTime.Now;
                cp.AddressInfo = new AddressInfo();

                var locationDetails = item["ChargeDeviceLocation"];
                var addressDetails = locationDetails["Address"];

            
                cp.AddressInfo.RelatedURL = "";
                cp.DateLastStatusUpdate = DateTime.Now;
                cp.AddressInfo.AddressLine1 = addressDetails["Street"].ToString().Replace("<br>",", ");
                cp.AddressInfo.Title = String.IsNullOrEmpty(locationDetails["LocationShortDescription"].ToString()) ? cp.AddressInfo.AddressLine1 : locationDetails["LocationShortDescription"].ToString();
                cp.AddressInfo.Title = cp.AddressInfo.Title.Replace("&amp;", "&");
                cp.AddressInfo.Title = cp.AddressInfo.Title.Replace("<br>", ", ");
                cp.AddressInfo.Town = addressDetails["PostTown"].ToString();
                cp.AddressInfo.StateOrProvince = addressDetails["DependantLocality"].ToString();
                cp.AddressInfo.Postcode = addressDetails["PostCode"].ToString();
                cp.AddressInfo.Latitude = double.Parse(locationDetails["Latitude"].ToString());
                cp.AddressInfo.Longitude = double.Parse(locationDetails["Longitude"].ToString());
                cp.AddressInfo.AccessComments = locationDetails["LocationLongDescription"].ToString().Replace("<br>", ", "); 
                //cp.AddressInfo.ContactTelephone1 = item["phone"].ToString();

                if (!String.IsNullOrEmpty(addressDetails["Country"].ToString()))
                {
                    string country = addressDetails["Country"].ToString();
                    int? countryID = null;

                    var countryVal = coreRefData.Countries.FirstOrDefault(c => c.Title.ToLower() == country.Trim().ToLower());
                    if (countryVal == null)
                    {
                        country = country.ToUpper();
                        //match country
                        if (country == "gb" || country == "US" || country == "USA" || country == "U.S." || country == "U.S.A.") countryID = 2;
                        if (country == "UK" || country == "GB" || country == "GREAT BRITAIN" || country == "UNITED KINGDOM") countryID = 1;
                    }
                    else
                    {
                        countryID = countryVal.ID;
                    }

                    if (countryID == null)
                    {
                        this.Log("Country Not Matched, will require Geolocation:" + item["country"].ToString());

                    }
                    else
                    {
                        cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(cy => cy.ID == countryID);
                    }
                }
                else
                {
                    //default to US if no country identified
                    //cp.AddressInfo.Country = cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(cy => cy.ID == 2);
                }

                //operator from DeviceController
                var deviceController = item["DeviceController"];

                cp.AddressInfo.RelatedURL = deviceController["Website"].ToString();
                var deviceOperator = coreRefData.Operators.FirstOrDefault(devOp => devOp.Title.Contains(deviceController["OrganisationName"].ToString()));
                if (deviceOperator != null)
                {
                    cp.OperatorInfo = deviceOperator;
                }
                else
                {
                    //operator from device owner
                    var devOwner = item["DeviceOwner"];
                    deviceOperator = coreRefData.Operators.FirstOrDefault(devOp => devOp.Title.Contains(devOwner["OrganisationName"].ToString()));
                    if (deviceOperator != null)
                    {
                        cp.OperatorInfo = deviceOperator;
                    }
                }

                //determine most likely usage type
                cp.UsageType = usageTypePrivate;

                if (item["SubscriptionRequiredFlag"].ToString().ToUpper() == "TRUE") cp.UsageType = usageTypePublicMembershipRequired;

                //add connections
                var connectorList = item["Connector"].ToArray();
                foreach (var conn in connectorList)
                {
                    string connectorType = conn["ConnectorType"].ToString();
                    if (!String.IsNullOrEmpty(connectorType))
                    {
                        ConnectionInfo cinfo = new ConnectionInfo() { };
                        ConnectionType cType = new ConnectionType { ID = 0 };
                        ChargerType level = null;
                        cinfo.Reference = conn["ConnectorId"].ToString();

                        if (connectorType.ToUpper().Contains("BS 1363"))
                        {
                            cType = new ConnectionType();
                            cType.ID = 3; //UK 13 amp plug
                            level = new ChargerType { ID = 2 };//default to level 2
                        }

                        if (connectorType.ToUpper() == "IEC 62196-2 TYPE 1 (SAE J1772)")
                        {
                            cType = new ConnectionType();
                            cType.ID = 2; //J1772
                            level = new ChargerType { ID = 2 };//default to level 2
                        }

                        if (connectorType.ToUpper() == "IEC 62196-2 TYPE 2")
                        {
                            cType = new ConnectionType();
                            cType.ID = 25; //Mennkes Type 2
                            level = new ChargerType { ID = 2 };//default to level 2
                        }

                        if (connectorType.ToUpper() == "JEVS G 105 (CHADEMO)")
                        {
                            cType = new ConnectionType();
                            cType.ID = 2; //CHadeMO
                            level = new ChargerType { ID = 3 };//default to level 3
                        }
                        if (connectorType.ToUpper() == "IEC 62196-2 TYPE 3")
                        {
                            cType = new ConnectionType();
                            cType.ID = 26; //IEC 62196-2 type 3

                            level = new ChargerType { ID = 2 };//default to level 2
                        }

                        if (cType.ID == 0)
                        {
                            var conType = coreRefData.ConnectionTypes.FirstOrDefault(ct => ct.Title.ToLower().Contains(conn.ToString().ToLower()));
                            if (conType != null) cType = conType;
                        }

                        if (!String.IsNullOrEmpty(conn["RatedOutputVoltage"].ToString())) cinfo.Voltage = int.Parse(conn["RatedOutputVoltage"].ToString());
                        if (!String.IsNullOrEmpty(conn["RatedOutputCurrent"].ToString())) cinfo.Amps = int.Parse(conn["RatedOutputCurrent"].ToString());
                        //TODO: use AC/DC/3 Phase data

                        if (conn["ChargePointStatus"] != null)
                        {
                            cinfo.StatusType = operationalStatus;
                            if (conn["ChargePointStatus"].ToString() == "Out of service") cinfo.StatusType = nonoperationalStatus;
                        }
                        
                        cinfo.ConnectionType = cType;
                        cinfo.Level = level;

                        if (cp.Connections == null)
                        {
                            cp.Connections = new List<ConnectionInfo>();
                            if (!IsConnectionInfoBlank(cinfo))
                            {
                                cp.Connections.Add(cinfo);
                            }
                        }
                    }
                }

                if (cp.DataQualityLevel == null) cp.DataQualityLevel = 3;

                cp.SubmissionStatus = submissionStatus;

                outputList.Add(cp);
                itemCount++;
            }

            return outputList;
        }
    }
}
