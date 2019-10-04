using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;

namespace OCM.Import.Providers
{
    public class ImportProvider_AddEnergie : BaseImportProvider, IImportProviderWithInit
    {
        protected string ServiceBaseURL = null;
        protected string ServiceUserName = null;
        protected string ServicePassword = null;
        protected NetworkType SelectedNetworkType = NetworkType.ReseauVER;

        public bool IsInitialised { get; set; }

        public enum NetworkType
        {
            ReseauVER,
            LeCircuitElectrique
        }

        public ImportProvider_AddEnergie(NetworkType network)
        {
            this.SelectedNetworkType = network;

            ServiceUserName = "OpenChargeMap";
            ProviderName = "AddEnergie";

            if (network == NetworkType.ReseauVER)
            {
                ServiceBaseURL = "https://admin.reseauver.com";
                ServicePassword = System.Configuration.ConfigurationManager.AppSettings["ImportProviderAPIKey_AddEnergieReseauVER"];
                ProviderName += " [ReseauVER]";
                DataProviderID = 24;
            }

            if (network == NetworkType.LeCircuitElectrique)
            {
                ServiceBaseURL = "https://lecircuitelectrique.co";
                ServicePassword = System.Configuration.ConfigurationManager.AppSettings["ImportProviderAPIKey_AddEnergieLeCircuitElectrique"];
                ProviderName += " [LeCircuitElectrique]";
                DataProviderID = 24;
            }

            AutoRefreshURL = ServiceBaseURL + "/Network/StationsList";

            OutputNamePrefix = "AddEnergie_" + network.ToString();

            IsAutoRefreshed = true;
            IsProductionReady = true;

            SourceEncoding = Encoding.GetEncoding("UTF-8");

            //this provider requires InitImportProvider to be called
            ImportInitialisationRequired = true;
        }

        public void InitImportProvider()
        {
            //login and get authentications cookie headers
            try
            {
                webClient.DownloadString(ServiceBaseURL + "/default.aspx?username=" + ServiceUserName + "&password=" + ServicePassword);

                IsInitialised = true;
                ImportInitialisationRequired = false;
            }
            catch (Exception)
            {
                IsInitialised = false;
            }
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
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
            var dataList = o.Values()["list"].Values().ToArray();

            foreach (var item in dataList)
            {
                ChargePoint cp = new ChargePoint();
                cp.DataProvider = new DataProvider() { ID = this.DataProviderID }; //AddEnergie
                cp.DataProvidersReference = item["StationID"].ToString();
                cp.DateLastStatusUpdate = DateTime.UtcNow;

                cp.AddressInfo = new AddressInfo();

                cp.AddressInfo.Title = item["ParkName"].ToString();
                cp.AddressInfo.AddressLine1 = item["Address"].ToString().Trim();
                cp.AddressInfo.Town = item["City"].ToString().Trim();
                cp.AddressInfo.StateOrProvince = item["StateOrProvince"].ToString().Trim();
                cp.AddressInfo.Postcode = item["PostalOrZipCode"].ToString().Trim();
                cp.AddressInfo.Latitude = double.Parse(item["Latitude"].ToString());
                cp.AddressInfo.Longitude = double.Parse(item["Longitude"].ToString());

                //default to canada
                //cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(c => c.ISOCode.ToLower() == "ca");
                //todo: detect country

                //set network operators
                if (this.SelectedNetworkType == NetworkType.ReseauVER)
                {
                    cp.OperatorInfo = new OperatorInfo { ID = 89 };
                }

                if (this.SelectedNetworkType == NetworkType.LeCircuitElectrique)
                {
                    cp.OperatorInfo = new OperatorInfo { ID = 90 };
                }

                bool isPublic = bool.Parse(item["IsPublic"].ToString());
                if (isPublic)
                {
                    cp.UsageType = usageTypePublic;
                }
                else
                {
                    cp.UsageType = usageTypePrivate;
                }

                cp.NumberOfPoints = int.Parse(item["NumPorts"].ToString());
                cp.StatusType = operationalStatus;

                //populate connectioninfo from Ports
                foreach (var port in item["Ports"].ToArray())
                {
                    ConnectionInfo cinfo = new ConnectionInfo() { };
                    ConnectionType cType = new ConnectionType { ID = 0 };

                    cinfo.Amps = int.Parse(port["Current"].ToString());
                    cinfo.Voltage = int.Parse(port["Voltage"].ToString());
                    cinfo.PowerKW = double.Parse(port["KiloWatts"].ToString());
                    cinfo.Level = new ChargerType() { ID = int.Parse(port["Level"].ToString()) };
                    //cinfo.Comments = (port["Make"]!=null?port["Make"].ToString()+" ":"") + (port["Model"]!=null?port["Model"].ToString():"");

                    if (port["ConnectorType"].ToString() == "J1772")
                    {
                        cType = coreRefData.ConnectionTypes.FirstOrDefault(c => c.ID == 1);
                    }
                    else if (port["ConnectorType"].ToString().ToUpper() == "CHADEMO")
                    {
                        cType = coreRefData.ConnectionTypes.FirstOrDefault(c => c.ID == 2);//CHADEMO
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Unmatched connector" + port["ConnectorType"].ToString());
                    }

                    cinfo.ConnectionType = cType;

                    if (cp.Connections == null)
                    {
                        cp.Connections = new List<ConnectionInfo>();
                        if (!IsConnectionInfoBlank(cinfo))
                        {
                            cp.Connections.Add(cinfo);
                        }
                    }
                }

                if (cp.DataQualityLevel == null) cp.DataQualityLevel = 4;

                cp.SubmissionStatus = submissionStatus;

                outputList.Add(cp);
                itemCount++;
            }

            return outputList;
        }
    }
}