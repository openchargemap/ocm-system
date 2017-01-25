using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCM.Import.Providers
{
    public class ImportProvider_AFDC : BaseImportProvider, IImportProvider
    {
        public ImportProvider_AFDC()
        {
            ProviderName = "afdc.energy.gov";
            OutputNamePrefix = "afdc";
            AutoRefreshURL = "http://developer.nrel.gov/api/alt-fuel-stations/v1.json?access=all&api_key=df771c4ffab663f91428bc63224c9e266357179d&download=true&fuel_type=ELEC&status=all";
            IsAutoRefreshed = true;
            IsProductionReady = true;
            DataProviderID = 2; //ADFC
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
            List<ChargePoint> outputList = new List<ChargePoint>();

            string source = InputData;

            JObject o = JObject.Parse(source);

            var dataList = o["fuel_stations"].ToArray();

            var submissionStatus = coreRefData.SubmissionStatusTypes.First(s => s.ID == 100);//imported and published
            var submissionStatusDelistedDupe = coreRefData.SubmissionStatusTypes.First(s => s.ID == 1001); //delisted duplicate
            var operationalStatus = coreRefData.StatusTypes.First(os => os.ID == 50);
            var nonoperationalStatus = coreRefData.StatusTypes.First(os => os.ID == 100);
            var unknownStatus = coreRefData.StatusTypes.First(os => os.ID == 0);
            var usageTypePublic = coreRefData.UsageTypes.First(u => u.ID == 1);
            var usageTypePrivate = coreRefData.UsageTypes.First(u => u.ID == 2);
            var usageTypePublicPayAtLocation = coreRefData.UsageTypes.First(u => u.ID == 5);
            var usageTypePublicMembershipRequired = coreRefData.UsageTypes.First(u => u.ID == 4);
            var usageTypePublicNoticeRequired = coreRefData.UsageTypes.First(u => u.ID == 7);
            var operatorUnknown = coreRefData.Operators.First(opUnknown => opUnknown.ID == 1);
            var chrgLevel1 = coreRefData.ChargerTypes.First(c => c.ID == 1);
            var chrgLevel2 = coreRefData.ChargerTypes.First(c => c.ID == 2);
            var chrgLevel3 = coreRefData.ChargerTypes.First(c => c.ID == 3);

            int itemCount = 0;

            foreach (var dataItem in dataList)
            {
                bool skipItem = false;
                ChargePoint cp = new ChargePoint();

                try
                {
                    var item = dataItem;

                    cp.DataProviderID = this.DataProviderID; //AFDC
                    cp.DataProvidersReference = item["id"].ToString();
                    cp.DateLastStatusUpdate = DateTime.UtcNow;
                    cp.AddressInfo = new AddressInfo();

                    if (item["ev_network_web"] != null) cp.AddressInfo.RelatedURL = item["ev_network_web"].ToString();
                    cp.DateLastStatusUpdate = DateTime.UtcNow;
                    if (item["street_address"] != null) cp.AddressInfo.AddressLine1 = item["street_address"].ToString().Replace("<br>", ", ");
                    if (item["station_name"] != null) cp.AddressInfo.Title = item["station_name"].ToString();
                    cp.AddressInfo.Title = cp.AddressInfo.Title.Replace("&amp;", "&");
                    cp.AddressInfo.Title = cp.AddressInfo.Title.Replace("<br>", ", ");
                    if (item["city"] != null) cp.AddressInfo.Town = item["city"].ToString();
                    if (item["state"] != null) cp.AddressInfo.StateOrProvince = item["state"].ToString();
                    if (item["zip"] != null) cp.AddressInfo.Postcode = item["zip"].ToString();
                    if (item["latitude"] != null) cp.AddressInfo.Latitude = double.Parse(item["latitude"].ToString());
                    if (item["longitude"] != null) cp.AddressInfo.Longitude = double.Parse(item["longitude"].ToString());
                    if (item["access_days_time"] != null) cp.AddressInfo.AccessComments = item["access_days_time"].ToString().Replace("<br>", ", ");
                    if (item["date_last_confirmed"] != null && !String.IsNullOrEmpty(item["date_last_confirmed"].ToString()) && item["date_last_confirmed"].ToString() != "{}")
                    {
                        cp.DateLastConfirmed = DateTime.Parse(item["date_last_confirmed"].ToString());
                    }
                    if (item["station_phone"] != null) cp.AddressInfo.ContactTelephone1 = item["station_phone"].ToString();

                    int? countryID = null;
                    if (cp.AddressInfo.StateOrProvince != null && cp.AddressInfo.StateOrProvince.Length == 2)
                    {
                        //state specified, assume US
                        countryID = 2;
                    }

                    if (countryID == null)
                    {
                        this.Log("Country Not Matched, will require Geolocation:" + item["state"].ToString());
                    }
                    else
                    {
                        cp.AddressInfo.CountryID = countryID;
                    }

                    //operator from ev_network
                    string deviceController = item["ev_network"].ToString();

                    if (!String.IsNullOrEmpty(deviceController))
                    {
                        deviceController = deviceController.ToLower().Replace(" network", "");

                        var deviceOperatorInfo = coreRefData.Operators.FirstOrDefault(devOp => devOp.Title.ToLower().Contains(deviceController) == true);
                        if (deviceOperatorInfo != null)
                        {
                            cp.OperatorID = deviceOperatorInfo.ID;
                        }
                        else
                        {
                            this.Log("Unknown network operator:" + deviceController);
                        }
                    }

                    //determine most likely usage type
                    cp.UsageTypeID = usageTypePrivate.ID;
                    if (item["groups_with_access_code"] != null)
                    {
                        string accessDesc = item["groups_with_access_code"].ToString().ToLower();
                        if (accessDesc.StartsWith("public - see hours"))
                        {
                            cp.UsageTypeID = usageTypePublic.ID;
                        }
                        else if (accessDesc.Trim() == "public")
                        {
                            cp.UsageTypeID = usageTypePublic.ID;
                        }
                        else if (accessDesc.StartsWith("private access only"))
                        {
                            cp.UsageTypeID = usageTypePrivate.ID;
                        }
                        else if (accessDesc.StartsWith("public - card key at all times"))
                        {
                            cp.UsageTypeID = usageTypePublicMembershipRequired.ID;
                        }
                        else if (accessDesc.StartsWith("public - call ahead"))
                        {
                            cp.UsageTypeID = usageTypePublicNoticeRequired.ID;
                        }
                        else if (accessDesc.StartsWith("public - credit card at all times"))
                        {
                            cp.UsageTypeID = usageTypePublicPayAtLocation.ID;
                        }
                        else if (accessDesc.StartsWith("private"))
                        {
                            cp.UsageTypeID = usageTypePrivate.ID;
                        }
                        else if (accessDesc.StartsWith("planned"))
                        {
                            cp.UsageTypeID = usageTypePrivate.ID;
                            cp.StatusTypeID = nonoperationalStatus.ID;
                            skipItem = true;
                        }
                        else
                        {
                            this.Log("Uknown usage type:" + item["groups_with_access_code"].ToString());
                        }
                    }

                    string ev_other_evse = null;
                    if (item["ev_other_evse"] != null) ev_other_evse = item["ev_other_evse"].ToString();
                    if (!String.IsNullOrEmpty(ev_other_evse))
                    {
                        cp.GeneralComments = ev_other_evse;
                    }
                    int numLevel1 = String.IsNullOrEmpty(item["ev_level1_evse_num"].ToString()) == false ? int.Parse(item["ev_level1_evse_num"].ToString()) : 0;
                    int numLevel2 = String.IsNullOrEmpty(item["ev_level2_evse_num"].ToString()) == false ? int.Parse(item["ev_level2_evse_num"].ToString()) : 0;
                    int numLevel3 = String.IsNullOrEmpty(item["ev_dc_fast_num"].ToString()) == false ? int.Parse(item["ev_dc_fast_num"].ToString()) : 0;
                    var evconnectors = item["ev_connector_types"].ToArray();
                    if (cp.Connections == null)
                    {
                        cp.Connections = new List<ConnectionInfo>();
                    }

                    if (numLevel1 > 0)
                    {
                        ConnectionInfo cinfo = new ConnectionInfo()
                        {
                            Quantity = numLevel1,
                            ConnectionTypeID = 0, //unknown
                            LevelID = chrgLevel1.ID,
                            //assume basic level 1 power
                            Voltage = 120,
                            Amps = 16,
                            CurrentTypeID = 10 //AC
                        };

                        cinfo.PowerKW = (cinfo.Voltage * cinfo.Amps) / 1000;

                        if (evconnectors.Any(c => c.Value<string>() == "NEMA520")) cinfo.ConnectionTypeID = 9; //nema 5-20

                        if (cinfo.ConnectionTypeID == 0 && evconnectors.Any())
                        {
                            //unknown connection type
                            Log("Unknown Lvl 1 Connection Type or top many types:" + evconnectors.ToString());
                        }

                        if (!IsConnectionInfoBlank(cinfo))
                        {
                            cp.Connections.Add(cinfo);
                        }
                    }

                    if (numLevel2 > 0)
                    {
                        ConnectionInfo cinfo = new ConnectionInfo()
                        {
                            Quantity = numLevel2,
                            ConnectionTypeID = 0, //unknown
                            LevelID = chrgLevel2.ID,
                            //assume basic level 2 power
                            Voltage = 230,
                            Amps = 16,
                            CurrentTypeID = 10 //AC
                        };

                        cinfo.PowerKW = (cinfo.Voltage * cinfo.Amps) / 1000;

                        if (evconnectors.Any(c => c.Value<string>() == "J1772")) cinfo.ConnectionTypeID = 1; //J1772

                        if (evconnectors.Any(c => c.Value<string>() == "TESLA"))
                        {
                            cinfo.ConnectionTypeID = 30; //tesla model S North America proprietary
                        }
                       
                        if (cinfo.ConnectionTypeID == 0 && evconnectors.Any())
                        {
                            //unknown connection type
                            Log("Unknown Lvl 2 Connection Type or too many types:" + evconnectors.ToString());
                        }

                        if (!IsConnectionInfoBlank(cinfo))
                        {
                            cp.Connections.Add(cinfo);
                        }
                    }

                    if (numLevel3 > 0)
                    {
                        //for each level 3 type connector identified, add an equipment entry. We don't have full information as data may say 3 * Lvl3, including CHADEMO & J1772COMBO, but we don't know which is which.
                        var allConnectors = evconnectors.Where(c => c.Value<string>() == "CHADEMO" || c.Value<string>() == "TESLA" || c.Value<string>() == "J1772COMBO");
                        bool lvl3Added = false;
                        foreach (var lvl3Connector in allConnectors)
                        {
                            int stdQuantity = numLevel3;
                            if (allConnectors.Count() > 1) stdQuantity = 1;

                            ConnectionInfo cinfo = new ConnectionInfo()
                            {
                                Quantity = stdQuantity,
                                ConnectionTypeID = 0, //unknown
                                LevelID = chrgLevel3.ID,

                                //assume basic level 3 power
                                Voltage = 400,
                                Amps = 100,
                                CurrentTypeID = 10 //DC
                            };

                            cinfo.PowerKW = (cinfo.Voltage * cinfo.Amps) / 1000;

                            if (lvl3Connector.Value<string>() == "CHADEMO")
                            {
                                cinfo.ConnectionTypeID = 2; //CHADEMO
                            }

                            if (lvl3Connector.Value<string>() == "TESLA")
                            {
                                cinfo.ConnectionTypeID = 27; //tesla supercharger
                            }

                            if (lvl3Connector.Value<string>() == "J1772COMBO")
                            {
                                cinfo.ConnectionTypeID = 32; //SAE Combo (DC Fast J1772 Version)
                            }

                            if (!IsConnectionInfoBlank(cinfo))
                            {
                                cp.Connections.Add(cinfo);
                            }
                            lvl3Added = true;
                        }

                        if (!lvl3Added)
                        {
                            //unknown connection type
                            Log("Unknown Lvl 3 Connection Type :" + evconnectors.ToString());
                        }
                    }

                    if (cp.DataQualityLevel == null) cp.DataQualityLevel = 3;

                    cp.SubmissionStatus = submissionStatus;
                }
                catch (Exception exp)
                {
                    Log("Exception parsing imported item " + itemCount + ":" + exp.ToString());
                    skipItem = true;
                }
                if (!skipItem) outputList.Add(cp);

                itemCount++;
            }

            return outputList.ToList();
        }
    }
}
