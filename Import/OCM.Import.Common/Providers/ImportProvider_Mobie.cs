using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace OCM.Import.Providers
{
    public class ImportProvider_Mobie : BaseImportProvider, IImportProvider
    {
        public ImportProvider_Mobie()
        {
            ProviderName = "mobie.pt";
            OutputNamePrefix = "mobie";
            AutoRefreshURL = "http://www.mobie.pt/en/postos-de-carregamento?p_p_id=googlemaps_WAR_mobiebusinessportlet_INSTANCE_SsJ4&p_p_lifecycle=2&p_p_state=normal&p_p_mode=view&p_p_resource_id=searchPoles&p_p_cacheability=cacheLevelPage&p_p_col_id=column-1&p_p_col_pos=1&p_p_col_count=2&searchString=";

            IsAutoRefreshed = true;
            IsProductionReady = true;
            SourceEncoding = Encoding.GetEncoding("UTF-8");
            DataProviderID = 7;//mobie.pt
        }

        List<ChargePoint> IImportProvider.Process(CoreReferenceData coreRefData)
        {
            var submissionStatus = coreRefData.SubmissionStatusTypes.First(s => s.ID == 100);//imported and published
            var status_operational = coreRefData.StatusTypes.First(os => os.ID == 50);
            var status_notoperational = coreRefData.StatusTypes.First(os => os.ID == 100);

            var status_operationalMixed = coreRefData.StatusTypes.First(os => os.ID == 75);
            var status_available = coreRefData.StatusTypes.First(os => os.ID == 10);
            var status_inuse = coreRefData.StatusTypes.First(os => os.ID == 20);
            var status_unknown = coreRefData.StatusTypes.First(os => os.ID == 0);
            var usageTypePublic = coreRefData.UsageTypes.First(u => u.ID == 1);
            var usageTypePrivate = coreRefData.UsageTypes.First(u => u.ID == 2);

            JavaScriptSerializer jss = new JavaScriptSerializer();
            jss.RegisterConverters(new JavaScriptConverter[] { new DynamicJsonConverter() });

            dynamic glossaryEntry = jss.Deserialize(InputData, typeof(object)) as dynamic;
            var dataList = glossaryEntry.response.data;

            List<ChargePoint> outputList = new List<ChargePoint>();

            int itemCount = 0;

            foreach (var item in dataList)
            {
                try
                {
                    ChargePoint cp = new ChargePoint();
                    cp.AddressInfo = new AddressInfo();

                    cp.AddressInfo.Title = item["street"] != null ? item["street"].ToString() : item["chargingStationId"].ToString();
                    cp.AddressInfo.RelatedURL = "http://www.mobie.pt";

                    cp.DataProvider = new DataProvider() { ID = this.DataProviderID }; //mobie.pt
                    cp.DataProvidersReference = item["chargingStationId"].ToString();
                    cp.DateLastStatusUpdate = DateTime.UtcNow;

                    cp.AddressInfo.AddressLine1 = item["street"].ToString();
                    cp.AddressInfo.Town = item["city"].ToString();

                    cp.AddressInfo.Postcode = item["postalCode"].ToString();
                    cp.AddressInfo.Latitude = double.Parse(item["latitude"].ToString());
                    cp.AddressInfo.Longitude = double.Parse(item["longitude"].ToString());

                    cp.NumberOfPoints = int.Parse(item["numberOfSattelites"].ToString());

                    cp.StatusType = status_operational;
                    string status = item["status"].ToString().ToLower();

                    if (status == "unavailable" || status == "reserved" || status == "in use")
                    {
                        cp.StatusType = status_operational;
                    }

                    if (status == "disconnected" || status == "inactive" || status == "suspended")
                    {
                        cp.StatusType = status_notoperational;
                    }

                    string type = item["type"].ToString();//fast or normal
                    if (type.ToLower() == "fast" || type.ToLower() == "normal")
                    {
                        //populate connections
                        cp.Connections = new List<ConnectionInfo>();

                        ConnectionInfo con = new ConnectionInfo();
                        if (String.Equals(type, "fast", StringComparison.CurrentCultureIgnoreCase))
                        {
                            con.Level = new ChargerType { ID = 3 };
                            con.Voltage = 400;
                            con.Amps = 75;
                            con.PowerKW = con.Voltage * con.Amps / 1000;
                            con.StatusType = cp.StatusType;
                            con.CurrentType = new CurrentType { ID = (int)StandardCurrentTypes.DC };
                        }

                        if (String.Equals(type, "normal", StringComparison.CurrentCultureIgnoreCase))
                        {
                            con.Level = new ChargerType { ID = 2 };
                            //based on http://www.mobie.pt/en/o-carregamento
                            con.Voltage = 220;
                            con.Amps = 16;
                            con.PowerKW = con.Voltage * con.Amps / 1000;
                            con.StatusType = cp.StatusType;
                            con.CurrentType = new CurrentType { ID = (int)StandardCurrentTypes.SinglePhaseAC };
                        }
                        cp.Connections.Add(con);
                    }

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