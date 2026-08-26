using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.AFDC
{
    public class ImportProvider_AFDC : BaseImportProvider, IImportProvider
    {
        private const int UnknownOperator = 1;
        
        public ImportProvider_AFDC(string apiKey)
        {
            ProviderName = "afdc.energy.gov";
            OutputNamePrefix = "afdc";
            ApiKey = apiKey;
            AutoRefreshURL = $"https://developer.nlr.gov/api/alt-fuel-stations/v1.json?access=all&api_key={apiKey}&download=true&fuel_type=ELEC&status=all&country=US,CA";
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
            var usageTypePublic = coreRefData.UsageTypes.First(u => u.ID == 1);
            var usageTypePrivate = coreRefData.UsageTypes.First(u => u.ID == 2);
            var usageTypePublicPayAtLocation = coreRefData.UsageTypes.First(u => u.ID == 5);
            var usageTypePublicMembershipRequired = coreRefData.UsageTypes.First(u => u.ID == 4);
            var usageTypePublicNoticeRequired = coreRefData.UsageTypes.First(u => u.ID == 7);
            var chargerLevel1 = coreRefData.ChargerTypes.First(c => c.ID == 1);
            var chargerLevel2 = coreRefData.ChargerTypes.First(c => c.ID == 2);
            var chargerLevel3 = coreRefData.ChargerTypes.First(c => c.ID == 3);
            
            var operatorIdMapper = OperatorIdMapper.Create();
            var connectionsMapper = new ConnectionsMapper();

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

                    MapCountry(item, cp);
                    MapOperator(item, cp, operatorIdMapper);

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

                    string ev_other_evse = null;
                    if (item["ev_other_evse"] != null) ev_other_evse = item["ev_other_evse"].ToString();
                    if (!String.IsNullOrEmpty(ev_other_evse))
                    {
                        cp.GeneralComments = ev_other_evse;
                    }

                    cp.Connections = connectionsMapper.Map(item["ev_charging_units"].ToArray(),
                        chargerLevel1, chargerLevel2, chargerLevel3);

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

        private void MapOperator(JToken item, ChargePoint cp, Dictionary<string, int> operatorIdMapper)
        {
            string evNetwork = item["ev_network"].ToString();
            cp.OperatorID = operatorIdMapper.GetValueOrDefault(evNetwork, UnknownOperator);
            if (cp.OperatorID == UnknownOperator) {
                this.Log("Unknown network operator:" + evNetwork);
            }
        }

        private void MapCountry(JToken item, ChargePoint cp)
        {
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
        }
    }
}
