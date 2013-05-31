using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCM.API.Common.Model;
using Newtonsoft.Json.Linq;

namespace OCM.Import.Providers
{
    public class ImportProvider_CarStations : BaseImportProvider, IImportProvider
    {
        public ImportProvider_CarStations()
        {
            ProviderName = "carstations.com";
            OutputNamePrefix = "carstations";
            AutoRefreshURL = "http://carstations.com/jsonresults.txt";
            IsAutoRefreshed = true;
            IsProductionReady = true;
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
            List<ChargePoint> outputList = new List<ChargePoint>();

            string source = InputData;

            JObject o = JObject.Parse(source);

            var dataList = o["locations"].ToArray();

            var submissionStatus = coreRefData.SubmissionStatusTypes.First(s => s.ID == 100);//imported and published
            var operationalStatus = coreRefData.StatusTypes.First(os => os.ID == 50);
            var unknownStatus = coreRefData.StatusTypes.First(os => os.ID == 0);
            var usageTypePublic = coreRefData.UsageTypes.First(u => u.ID == 1);
            var usageTypePrivate = coreRefData.UsageTypes.First(u => u.ID == 2);
            var operatorUnknown = coreRefData.Operators.First(opUnknown => opUnknown.ID == 1);

            int itemCount = 0;
            foreach (var item in dataList)
            {
                ChargePoint cp = new ChargePoint();
                cp.DataProvider = new DataProvider() { ID = 15 }; //carstations.com
                cp.DataProvidersReference = item["post_id"].ToString();
                cp.DateLastStatusUpdate = DateTime.Now;
                cp.AddressInfo = new AddressInfo();

                //carstations.com have requested we not use the station names from their data, so we use address
                //cp.AddressInfo.Title = item["name"] != null ? item["name"].ToString() : item["address"].ToString();
                cp.AddressInfo.Title = item["address"] != null ? item["address"].ToString() : item["post_id"].ToString();
                cp.AddressInfo.Title = cp.AddressInfo.Title.Trim().Replace("&amp;", "&");
                cp.AddressInfo.RelatedURL = "http://carstations.com/" + cp.DataProvidersReference;
                cp.DateLastStatusUpdate = DateTime.Now;
                cp.AddressInfo.AddressLine1 = item["address"].ToString().Trim();
                cp.AddressInfo.Town = item["city"].ToString().Trim();
                cp.AddressInfo.StateOrProvince = item["region"].ToString().Trim();
                cp.AddressInfo.Postcode = item["postal_code"].ToString().Trim();
                cp.AddressInfo.Latitude = double.Parse(item["latitude"].ToString().Trim());
                cp.AddressInfo.Longitude = double.Parse(item["longitude"].ToString().Trim());

                cp.AddressInfo.ContactTelephone1 = item["phone"].ToString();

                if (!String.IsNullOrEmpty(item["country"].ToString()))
                {
                    string country = item["country"].ToString();
                    int? countryID = null;

                    var countryVal = coreRefData.Countries.FirstOrDefault(c => c.Title.ToLower() == country.Trim().ToLower());
                    if (countryVal == null)
                    {
                        country = country.ToUpper();
                        //match country
                        if (country == "UNITED STATES" || country == "US" || country == "USA" || country == "U.S." || country == "U.S.A.") countryID = 2;

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

                //System.Diagnostics.Debug.WriteLine(item.ToString());
                string publicCount = item["public"].ToString();
                string privateCount = item["private"].ToString();

                if (!String.IsNullOrEmpty(publicCount) && publicCount != "0")
                {
                    try
                    {
                        cp.NumberOfPoints = int.Parse(publicCount);
                    }
                    catch (Exception) { }
                    cp.UsageType = usageTypePublic;
                }
                else
                {
                    if (!String.IsNullOrEmpty(privateCount) && privateCount != "0")
                    {
                        try
                        {
                            cp.NumberOfPoints = int.Parse(privateCount);
                        }
                        catch (Exception) { }
                        cp.UsageType = usageTypePrivate;
                    }
                }

                string verifiedFlag = item["verified_flag"].ToString();

                if (!string.IsNullOrEmpty(verifiedFlag) && verifiedFlag != "0")
                {
                    cp.StatusType = operationalStatus;
                }
                else
                {
                    cp.StatusType = unknownStatus;
                }

                //TODO: allow for multiple operators?
                var operatorsNames = item["brands"].ToArray();

                if (operatorsNames.Count() > 0)
                {
                    var operatorName = operatorsNames[0].ToString();
                    var opDetails = coreRefData.Operators.FirstOrDefault(op => op.Title.ToLower().Contains(operatorName.ToString().ToLower()));
                    if (opDetails != null)
                    {
                        cp.OperatorInfo = opDetails;
                    }
                    else
                    {
                        Log("Operator not matched:" + operatorName);
                    }

                }
                else
                {
                    cp.OperatorInfo = operatorUnknown;
                }

                var connectorTypes = item["techs"].ToArray();
                foreach (var conn in connectorTypes)
                {
                    ConnectionInfo cinfo = new ConnectionInfo() { };
                    ConnectionType cType = new ConnectionType { ID = 0 };
                    ChargerType level = null;
                    cinfo.Reference = conn.ToString();

                    if (conn.ToString().ToUpper() == "J1772")
                    {
                        cType = new ConnectionType();
                        cType.ID = 1; //J1772
                        level = new ChargerType { ID = 2 };//default to level 2
                    }

                    if (conn.ToString().ToUpper() == "CHADEMO")
                    {
                        cType = new ConnectionType();
                        cType.ID = 2; //CHadeMO
                        level = new ChargerType { ID = 3 };//default to level 3
                    }
                    if (conn.ToString().ToUpper() == "NEMA5")
                    {
                        cType = new ConnectionType();
                        cType.ID = 9; //NEMA5-20R
                        level = new ChargerType { ID = 1 };//default to level 1
                    }

                    if (cType.ID == 0)
                    {
                        var conType = coreRefData.ConnectionTypes.FirstOrDefault(ct => ct.Title.ToLower().Contains(conn.ToString().ToLower()));
                        if (conType != null) cType = conType;
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

                if (cp.DataQualityLevel == null) cp.DataQualityLevel = 2;

                cp.SubmissionStatus = submissionStatus;

                outputList.Add(cp);
                itemCount++;
            }

            return outputList;
        }
    }
}
