using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCM.API.Common.Model;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;

namespace OCM.Import.Providers
{
    public class ImportProvider_NobilDotNo : BaseImportProvider, IImportProvider
    {
        public ImportProvider_NobilDotNo()
        {
            ProviderName = "nobil.no";
            OutputNamePrefix = "nobildotno";
            AutoRefreshURL = "http://nobil.no/api/server/search.php";
            IsAutoRefreshed = false;
            IsProductionReady = true;
            SourceEncoding = Encoding.GetEncoding("UTF-8");
            DataProviderID = 19;//nobil.no
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
            List<ChargePoint> outputList = new List<ChargePoint>();

            string jsString = "{ \"data\": " + InputData + "}";

            JavaScriptSerializer jss = new JavaScriptSerializer();
            jss.RegisterConverters(new JavaScriptConverter[] { new DynamicJsonConverter() });
            dynamic parsedList = jss.Deserialize(jsString, typeof(object)) as dynamic;
            var dataList = parsedList.data;

            /*
             * [
               {
                  "id":189,
                  "navn":"DFDS til K\u00f8benhavn",
                  "adresse":"Vippetangen Utstikker
            II",
                  "postnr":"0150",
                  "poststed":"OSLO",
                  "kommunenr":"0301",
                  "kommunenavn":"OSLO
            ",
                  "stedsbeskrivelse":"",
                  "plasstype_navn":"Parkeringshus",
                  "tilgjengelighet_navn":"Offentlig",
                  "eier":"DFDS
            Seaways",
                  "antall_ladepunkter":2,
                  "parkeringsavgift":false,
                  "tidsbegrensing":12,
                  "la
            defart":"16A",
                  "posisjon":"(59.90349,10.74334)",
                  "utbyggingstotte_navn":"Ingen",
                  "b
            ilde":"189.jpg",
                  "ledigeplasser":2,
                  "kommentarbruker":"DFDS har lademuligheter for
            biler p\u00e5 overfart til\/fra Danmark. Gi beskjed ved booking og til
            mannskapet i fergen.",
                  "kontaktinfo":"Kundesenter 21 62 13 40",
                  "opprettet":"2010-
            03-26
            18:03:46",
                  "opprettetav_fornavn":"nobilAdmin",
                  "opprettetav_etternavn":"nobilAdmin
            ",
                  "endret":"2010-03-26
            18:06:35",
                  "endretav_fornavn":"nobilAdmin",
                  "endretav_etternavn":"nobilAdmin",
                  "til
            gang_navn":"\u00c5pen",
                  "oslokommune_eierid":""
               }
            ]
             * */

            var submissionStatus = coreRefData.SubmissionStatusTypes.First(s => s.ID == 100);//imported and published
            var operationalStatus = coreRefData.StatusTypes.First(os => os.ID == 50);
            var unknownStatus = coreRefData.StatusTypes.First(os => os.ID == 0);
            var usageTypePublic = coreRefData.UsageTypes.First(u => u.ID == 1);
            var usageTypePrivate = coreRefData.UsageTypes.First(u => u.ID == 2);
            var usageTypePrivateForStaffAndVisitors = coreRefData.UsageTypes.First(u => u.ID == 6); //staff and visitors
            var operatorUnknown = coreRefData.Operators.First(opUnknown => opUnknown.ID == 1);

            int itemCount = 0;
            foreach (var item in dataList)
            {
                ChargePoint cp = new ChargePoint();
                cp.DataProvider = new DataProvider() { ID = this.DataProviderID }; //nobil.no
                cp.DataProvidersReference = item["id"].ToString();
                cp.DateLastStatusUpdate = DateTime.UtcNow;
                cp.AddressInfo = new AddressInfo();

                //carstations.com have requested we not use the station names from their data, so we use address
                //cp.AddressInfo.Title = item["name"] != null ? item["name"].ToString() : item["address"].ToString();
                cp.AddressInfo.Title = item["navn"] != null ? item["navn"].ToString() : item["id"].ToString();
                cp.AddressInfo.Title = cp.AddressInfo.Title.Trim().Replace("&amp;", "&");
                cp.AddressInfo.RelatedURL = item["url"].ToString();

                cp.DateLastStatusUpdate = DateTime.UtcNow;
                cp.AddressInfo.AddressLine1 = item["adresse"].ToString().Trim();
                cp.AddressInfo.Town = item["kommunenavn"].ToString().Trim();
                cp.AddressInfo.StateOrProvince = item["kommunenavn"].ToString().Trim();
                cp.AddressInfo.Postcode = item["postnr"].ToString().Trim();
                string posString = item["posisjon"].ToString().Trim();
                int sepPos = posString.IndexOf(",") - 1;
                string lat = posString.Substring(1, sepPos);
                sepPos += 2;
                string lon = posString.Substring(sepPos, (posString.Length - sepPos) - 1);
                cp.AddressInfo.Latitude = double.Parse(lat);
                cp.AddressInfo.Longitude = double.Parse(lon);

                //default to norway
                cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(c => c.ISOCode.ToLower() == "no");
                //cp.AddressInfo.ContactTelephone1 = item["phone"].ToString();

                /*if (!String.IsNullOrEmpty(item["country"].ToString()))
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
                */
                //System.Diagnostics.Debug.WriteLine(item.ToString());
                string usageTypeString = item["tilgjengelighet_navn"].ToString();
                if (usageTypeString.ToLower() == "offentlig")
                {
                    cp.UsageType = usageTypePublic;
                }
                else if (usageTypeString.ToLower() == "besøkende")
                {
                    cp.UsageType = usageTypePrivateForStaffAndVisitors;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CP:" + cp.ID + " Unknown usage type:" + usageTypeString);
                    cp.UsageType = usageTypePrivate;
                }


                if (item["antall_ladepunkter"] != null) cp.NumberOfPoints = int.Parse(item["antall_ladepunkter"].ToString());
                cp.StatusType = unknownStatus;

                /*
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
                */
                string connPwr = item["ladefart"].ToString();

                ConnectionInfo cinfo = new ConnectionInfo() { };
                ConnectionType cType = new ConnectionType { ID = 0 };

                if (connPwr == "16A")
                {
                    cinfo.Amps = 16;
                    cinfo.ConnectionType = cType;
                    cinfo.Level = new ChargerType() { ID = 2 }; //default to lvl2
                }

                if (cp.Connections == null)
                {
                    cp.Connections = new List<ConnectionInfo>();
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
