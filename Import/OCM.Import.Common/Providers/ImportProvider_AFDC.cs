using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.Import.Providers
{
    public class ImportProvider_AFDC : BaseImportProvider, IImportProvider
    {
        public ImportProvider_AFDC(string apiKey)
        {
            ProviderName = "afdc.energy.gov";
            OutputNamePrefix = "afdc";
            ApiKey = apiKey;
            AutoRefreshURL = $"https://developer.nrel.gov/api/alt-fuel-stations/v1.json?access=all&api_key={apiKey}&download=true&fuel_type=ELEC&status=all&country=US,CA";
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
            int plannedItems = 0;

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
                    if (cp.AddressInfo.Title.Length > 100)
                    {
                        cp.AddressInfo.Title = cp.AddressInfo.Title.Substring(0, 100);
                    }
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

                    if (item["country"] != null)
                    {
                        if (item["country"].ToString() == "US")
                        {
                            cp.AddressInfo.CountryID = 2;
                        }
                        else if (item["country"].ToString() == "CA")
                        {
                            cp.AddressInfo.CountryID = 44;
                        }
                    }
                    else
                    {
                        this.Log("Unknown country code:" + item["country"]);
                    }

                    //operator from ev_network
                    string deviceController = item["ev_network"].ToString();

                    if (!String.IsNullOrEmpty(deviceController))
                    {
                        deviceController = deviceController.ToLower().Replace(" network", "");
                        if (deviceController == "circuit ã©lectrique" || deviceController == "circuit électrique")
                        {
                            deviceController = "circuit electrique";
                        }
                        else if (deviceController == "petrocan")
                        {
                            deviceController = "petro canada";
                        }

                        var deviceOperatorInfo = coreRefData.Operators.FirstOrDefault(devOp => devOp.Title.ToLower().Contains(deviceController) == true);
                        if (deviceOperatorInfo != null)
                        {
                            cp.OperatorID = deviceOperatorInfo.ID;
                        }
                        else if (deviceController != "non-networked")
                        {

                            switch (deviceController)
                            {
                                case "tesla destination":
                                    cp.OperatorID = (int)StandardOperators.Tesla;
                                    break;
                                case "shell_recharge":
                                    cp.OperatorID = 59;
                                    break;
                                case "rivian_waypoints":
                                    cp.OperatorID = 3617; //3607 = Rivian Adventure
                                    break;
                                case "rivian_adventure":
                                    cp.OperatorID = 3607; 
                                    break;
                                case "bchydro":
                                    cp.OperatorID = 3385;
                                    break;
                                case "powerflex":
                                    cp.OperatorID = 3618;
                                    break;
                                case "semacharge":
                                    cp.OperatorID = 39;
                                    break;
                                case "ampup":
                                    cp.OperatorID = 3619;
                                    break;
                                case "livingston":
                                    cp.OperatorID = 3620;
                                    break;
                                case "chargelab":
                                    cp.OperatorID = 3621;
                                    break;
                                case "universal":
                                    cp.OperatorID = 3694;
                                    break;
                                case "graviti_energy":
                                    cp.OperatorID = 3695;
                                    break;
                                case "evrange":
                                    cp.OperatorID = 3526;
                                    break;
                                case "zefnet":
                                    cp.OperatorID = 3454;
                                    break;
                                case "red_e":
                                    cp.OperatorID = 3696;
                                    break;
                                case "7charge":
                                    cp.OperatorID = 3697;
                                    break;
                                default:
                                    this.Log("Unknown network operator:" + deviceController);
                                    break;

                            }
                        }
                    }

                    //determine most likely usage type
                    cp.UsageTypeID = usageTypePrivate.ID;
                    if (item["access_code"] != null)
                    {
                        string accessCode = item["access_code"].ToString().ToLower();
                        if (accessCode.Equals("public"))
                        {
                            cp.UsageTypeID = usageTypePublic.ID;
                        }
                        else
                        {
                            cp.UsageTypeID = usageTypePrivate.ID;
                        }
                    }

                    if (cp.UsageTypeID == usageTypePublic.ID)
                    {
                        string accessDetail = item["access_detail_code"]?.ToString().ToLower();
                        if (!string.IsNullOrEmpty(accessDetail))
                        {
                            if (cp.AddressInfo.AccessComments == null) cp.AddressInfo.AccessComments = "";
                            else cp.AddressInfo.AccessComments += "\r\n";

                            if (accessDetail == "key_always")
                            {
                                // Card key at all times.
                                cp.AddressInfo.AccessComments += item["groups_with_access_code"]?.ToString();
                                cp.UsageTypeID = usageTypePublicMembershipRequired.ID;
                            }
                            else if (accessDetail == "credit_card_always")
                            {
                                // 	Credit card at all times.
                                cp.AddressInfo.AccessComments += item["groups_with_access_code"]?.ToString();
                                cp.UsageTypeID = usageTypePublicPayAtLocation.ID;
                            }
                            else if (accessDetail == "credit_card_after_hours")
                            {
                                // Credit card after hours.
                                cp.AddressInfo.AccessComments += item["groups_with_access_code"]?.ToString();
                                cp.UsageTypeID = usageTypePublicPayAtLocation.ID;
                            }
                            else if (accessDetail == "fleet")
                            {
                                // 	Fleet customers only.
                                cp.AddressInfo.AccessComments += item["groups_with_access_code"]?.ToString();
                                cp.UsageTypeID = usageTypePublicMembershipRequired.ID;
                            }
                            else if (accessDetail == "government")
                            {
                                // Government only.
                                cp.AddressInfo.AccessComments = item["groups_with_access_code"]?.ToString();
                                cp.UsageTypeID = usageTypePublicMembershipRequired.ID;
                            }
                            else if (accessDetail == "key_after_hours")
                            {
                                // Card key after hours.
                                cp.AddressInfo.AccessComments += item["groups_with_access_code"]?.ToString();
                                cp.UsageTypeID = usageTypePublicMembershipRequired.ID;
                            }
                            else if (accessDetail == "call")
                            {
                                // 	Call ahead.
                                cp.AddressInfo.AccessComments += item["groups_with_access_code"]?.ToString();
                                cp.UsageTypeID = usageTypePublicNoticeRequired.ID;
                            }

                        }

                    }

                    string status_code = item["status_code"]?.ToString().ToLower();
                    if (!string.IsNullOrEmpty(status_code))
                    {
                        if (status_code == "e")
                        {
                            cp.StatusTypeID = (int)StandardStatusTypes.Operational;
                        }
                        else if (status_code == "t")
                        {
                            cp.StatusTypeID = (int)StandardStatusTypes.TemporarilyUnavailable;
                            if (!string.IsNullOrEmpty(item["expected_date"]?.ToString()))
                            {
                                cp.DatePlanned = DateTime.Parse(item["expected_date"].ToString());
                            }
                        }
                        else if (status_code == "p")
                        {
                            cp.StatusTypeID = (int)StandardStatusTypes.PlannedForFutureDate;
                            if (!string.IsNullOrEmpty(item["expected_date"]?.ToString()))
                            {
                                cp.DatePlanned = DateTime.Parse(item["expected_date"].ToString());
                            }

                            // we set usage type to private for planned sites to reduce likelihood of people travelling to the location
                            cp.GeneralComments = "Planned for future date. Not Operational.";

                            cp.UsageTypeID = usageTypePrivate.ID;

                            plannedItems++;
                        }
                    }
                    /*

                    else if (accessDesc.StartsWith("private access only") || accessDesc.Contains("(private)"))
                    {
                        cp.UsageTypeID = usageTypePrivate.ID;
                    }
                    else if (accessDesc.StartsWith("public - card key at all times"))
                    {
                        cp.UsageTypeID = usageTypePublicMembershipRequired.ID;
                    }
                    else if (accessDesc.Contains("public - call ahead"))
                    {
                        cp.UsageTypeID = usageTypePublicNoticeRequired.ID;
                    }
                    else if (accessDesc.Contains("public - credit card at all times"))
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
                        this.Log("Unknown usage type:" + item["groups_with_access_code"].ToString());
                    }
                    }*/

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
                            Comments = "kW power is an estimate based on the connection type",
                            CurrentTypeID = 10 //AC
                        };

                        cinfo.PowerKW = ComputePowerkWForConnectionInfo(cinfo);

                        if (evconnectors.Any(c => c.Value<string>() == "NEMA520")) cinfo.ConnectionTypeID = (int)StandardConnectionTypes.Nema5_20;
                        if (evconnectors.Any(c => c.Value<string>() == "NEMA515")) cinfo.ConnectionTypeID = (int)StandardConnectionTypes.Nema5_15;
                        if (evconnectors.Any(c => c.Value<string>() == "NEMA1450")) cinfo.ConnectionTypeID = (int)StandardConnectionTypes.Nema14_50;

                        if (cinfo.ConnectionTypeID == 0 && evconnectors.Any())
                        {
                            //unknown connection type
                            Log("Unknown Lvl 1 Connection Type or too many types:" + evconnectors.ToString());
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
                            Comments= "kW power is an estimate based on the connection type",
                            CurrentTypeID = 10 //AC
                        };

                        cinfo.PowerKW = (double?)ComputePowerkWForConnectionInfo(cinfo);

                        if (evconnectors.Any(c => c.Value<string>() == "J1772")) cinfo.ConnectionTypeID = (int)StandardConnectionTypes.J1772; //J1772

                        if (evconnectors.Any(c => c.Value<string>() == "TESLA"))
                        {
                            cinfo.ConnectionTypeID = (int)StandardConnectionTypes.TeslaProprietary; //tesla model S North America proprietary
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
                        var allConnectors = evconnectors.Where(c => c.Value<string>() == "CHADEMO" || c.Value<string>() == "TESLA" || c.Value<string>() == "J1772COMBO").Select(c => c.Value<string>()).ToList();

                        if (evconnectors.Length == 1 && evconnectors[0].Value<string>() == "J1772")
                        {
                            allConnectors.Add("J1772COMBO");
                        }

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
                               // Voltage = 400,
                               // Amps = 100,
                                Comments="kW power is an estimate based on the connection type",
                                PowerKW = 50,
                                CurrentTypeID = (int)StandardCurrentTypes.DC //DC
                            };

                            cinfo.PowerKW = (double?)ComputePowerkWForConnectionInfo(cinfo);

                            if (lvl3Connector == "CHADEMO")
                            {
                                cinfo.ConnectionTypeID = 2; //CHADEMO
                            }

                            if (lvl3Connector == "TESLA")
                            {
                                cinfo.ConnectionTypeID = 27; //tesla supercharger
                            }

                            if (lvl3Connector == "J1772COMBO")
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

            Log($"Items Parsed:{outputList.Count} PlannedItems: {plannedItems}");

            return outputList.ToList();
        }
    }
}
