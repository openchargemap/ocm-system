using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace OCM.Import.Providers
{
    public class ImportProvider_PS : BaseImportProvider, IImportProvider
    {
        public ImportProvider_PS()
        {
            ProviderName = "PlugShare";
            OutputNamePrefix = "PlugShare_";

            IsAutoRefreshed = false;
            IsProductionReady = false;
            UseCustomReader = true;

            SourceEncoding = Encoding.GetEncoding("UTF-8");
            MergeDuplicatePOIEquipment = false;
            IncludeInvalidPOIs = true;
            AllowDuplicatePOIWithDifferentOperator = true;

            DataProviderID = 27; //PlugShare
        }

        /// <summary>
        /// http://stackoverflow.com/questions/9026508/incremental-json-parsing-in-c-sharp
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="readerStream"></param>
        /// <returns></returns>
        public static IEnumerable<T> DeserializeSequenceFromJson<T>(TextReader readerStream)
        {
            using (var reader = new JsonTextReader(readerStream))
            {
                var serializer = new JsonSerializer();
                if (!reader.Read() || reader.TokenType != JsonToken.StartArray)
                    throw new Exception("Expected start of array in the deserialized json string");

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.EndArray) break;
                    var item = serializer.Deserialize<T>(reader);
                    yield return item;
                }
            }
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

            var distinctCountries = new List<string>();

            var textReader = System.IO.File.OpenText(InputPath);
            foreach (var item in DeserializeSequenceFromJson<JObject>(textReader))
            {
                ChargePoint cp = new ChargePoint();
                cp.DataProvider = new DataProvider() { ID = this.DataProviderID };
                cp.DataProvidersReference = item["id"].ToString();
                cp.DateLastStatusUpdate = DateTime.UtcNow;

                cp.AddressInfo = new AddressInfo();

                cp.AddressInfo.Title = item["address"].ToString();
                cp.OperatorsReference = item["name"].ToString();

                cp.AddressInfo.AddressLine1 = item["address"].ToString().Trim();
                //cp.AddressInfo.Town = item["city"].ToString().Trim();
                //cp.AddressInfo.StateOrProvince = item["StateOrProvince"].ToString().Trim();
                //cp.AddressInfo.Postcode = item["postalcode"].ToString().Trim();
                cp.AddressInfo.Latitude = double.Parse(item["latitude"].ToString());
                cp.AddressInfo.Longitude = double.Parse(item["longitude"].ToString());

                // var countryCode = item["locale"].ToString().ToLower();

                //if (!distinctCountries.Exists(c => c == countryCode)) distinctCountries.Add(countryCode);

                //fix incorrect country codes

                //cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(c => c.ISOCode.ToLower() == countryCode);
                if (!String.IsNullOrEmpty(item["url"].ToString())) cp.AddressInfo.RelatedURL = item["url"].ToString();
                //if (!String.IsNullOrEmpty(item["email"].ToString())) cp.AddressInfo.ContactEmail = item["email"].ToString();
                //if (!String.IsNullOrEmpty(item["phone"].ToString())) cp.AddressInfo.ContactTelephone1 = item["phone"].ToString();

                /*var price = item["price"].ToString();
                var pricemethod = item["pricemethod"].ToString();

                cp.UsageCost = (!String.IsNullOrEmpty(price) ? price + " " : "") + pricemethod;
                //set network operators
                //cp.OperatorInfo = new OperatorInfo { ID = 89 };

                //TODO: Operator, usage,price, power, connector type
                var owner = item["owner"].ToString().ToLower();
                var operatoInfo = coreRefData.Operators.FirstOrDefault(op => op.Title.ToLower().Contains(owner));

                if (operatoInfo == null)
                {
                    Log("Unknown operator: " + owner);
                }
                else
                {
                    cp.OperatorID = operatoInfo.ID;
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
                */

                //cp.NumberOfPoints = int.Parse(item["nroutlets"].ToString());

                cp.StatusType = operationalStatus;

                //populate connectioninfo from Ports
                /*var connectorType = item["connectortype"].ToString();
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
                }
                else if (connectorType.ToLower().Contains("mennekes"))
                {
                    cinfo.ConnectionTypeID = (int)StandardConnectionTypes.MennekesType2;
                    cinfo.LevelID = 2;
                }
                else if (connectorType.ToLower().Contains("chademo"))
                {
                    cinfo.ConnectionTypeID = (int)StandardConnectionTypes.CHAdeMO;
                    cinfo.LevelID = 3;
                }
                else if (connectorType.ToLower().Contains("schuko"))
                {
                    cinfo.ConnectionTypeID = (int)StandardConnectionTypes.Schuko;
                    cinfo.LevelID = 2;
                }
                else
                {
                    Log("Unknown connectorType:" + connectorType);
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

                 * */
                if (cp.DataQualityLevel == null) cp.DataQualityLevel = 3;

                cp.SubmissionStatus = submissionStatus;
                var poiType = item["icon_type"].ToString();
                if (poiType != "H")
                {
                    outputList.Add(cp);
                }
                itemCount++;
            }

            /*private var distinctCountries = new List<string>();
            foreach (private var item in dataList)
            {
            private string temp = "";
            foreach (private var countryCode in distinctCountries)
            {
                temp += ", " + countryCode;
            }

            Log("Countries in import:" + temp);
             * */

            return outputList.Take(1000).ToList();
        }
    }
}