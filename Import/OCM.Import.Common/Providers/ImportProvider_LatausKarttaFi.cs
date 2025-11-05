using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OCM.Import.Providers
{
    public class ImportProvider_LatausKarttaFi : BaseImportProvider, IImportProvider
    {

        private static readonly string DefinitionsRefreshUrl = "https://latauskartta.fi/backend/actions.php?action=getMiscData";
        
        public ImportProvider_LatausKarttaFi()
        {
            ProviderName = "latauskartta.fi";
            OutputNamePrefix = "latauskarttafi";
            AutoRefreshURL = "https://latauskartta.fi/backend/actions.php?action=getData";
            IsAutoRefreshed = true;
            IsProductionReady = true;
            SourceEncoding = Encoding.GetEncoding("UTF-8");
            DataProviderID = 9999;//what is the id??
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
            Log("AAAAA");
            List<ChargePoint> outputList = new List<ChargePoint>();

            JObject o = JObject.Parse(InputData);

            JObject msg = (JObject) o.GetValue("msg");

            if (msg == null || !msg.ContainsKey("chargers") || !msg.ContainsKey("locations") || msg["locations"] == null || msg["chargers"] == null)
            {

                System.Diagnostics.Debug.WriteLine("ERR: No valid response from latauskartta.fi");
                throw new Exception("ERR: No valid response from latauskartta.fi");
                return outputList;
            }

            var loadDefinitionsTask = LoadInputFromURL(DefinitionsRefreshUrl);

            loadDefinitionsTask.Wait();

            if (!loadDefinitionsTask.Result)
            {
                System.Diagnostics.Debug.WriteLine("ERR: Cannot load definiitons from latauskartta.fi!");
                throw new Exception("ERR: Cannot load definiitons from latauskartta.fi!");
                return outputList;
            }
            
            JObject o2 = JObject.Parse(InputData);

            JObject defMsg = (JObject) o2.GetValue("msg");

            if (defMsg == null || !defMsg.ContainsKey("operators") || defMsg["operators"] == null)
            {

                System.Diagnostics.Debug.WriteLine("ERR: No valid definitions response from latauskartta.fi!");
                throw new Exception(InputData);
                return outputList;
            }

            var submissionStatus = coreRefData.SubmissionStatusTypes.First(s => s.ID == 100);//imported and published

            var statusMap = new Dictionary<string, int>
            {
                {"active", 50},
                {"disabled", 100},
                {"deleted", 200},
            };

            var plugTypesMap = new Dictionary<string, int>
            {
                {"1", 35}, // Red plug, IEC 60309 5 pin
                {"2", 25}, // Type 2 socket
                {"3", 2},  // CHAdeMO
                {"4", 33}, // CCS2
                {"8", 33}, // CCS2 but with label HPC
                {"9", 30}, // Tesla Supercharger modified Type2
                {"99", 30},// Unknown
            };

            // From: https://latauskartta.fi/backend/actions.php?action=getMiscData
            var existingOperators = new Dictionary<string, int>
            {
                {"0", 1},    // Free
                {"1", 198},  // Recharge
                {"2", 136},  // VIRTA
                {"3", 3354}, // K-Lataus
                {"4", 3793}, // eParking
                {"9", 3534}, // Tesla (assume including non-tesla by default)
                {"10", 1},   // Other
                {"12", 3302},// Easypark
                {"15", 3355},// Motonet
                {"16", 3353},// Plugit
                {"18", 3322},// Parking Energy
                {"19", 3299},// Ionity
                {"20", 3266},// Plugsurfing
                {"27", 3506},// ABC Lataus
                {"28", 3596},// Neste Lataus
                {"29", 3548},// Cloud charge
                {"31", 63},  // Greenflux
                {"33", 38},  // LIDL
                {"39", 3444},// EWAYS
                {"40", 136}, // Autoliitto aka. VIRTA
                {"44", 103}, // Allego
                {"45", 95},  // Helen
                {"48", 3479},// Monta
            };

            // Search for missing operators
            foreach (JObject chgOper in ((JObject)defMsg.GetValue("operators")).Values().OfType<JObject>())
            {
                if (chgOper.ContainsKey("id") && chgOper.ContainsKey("name") && chgOper["id"] != null &&
                    chgOper["name"] != null)
                {
                    var id = (string) chgOper["id"];
                    var name = (string) chgOper["name"];
                    var webpage = (string) chgOper["webpage"];
                    if (webpage == null)
                        webpage = "";
                    if (name == null)
                        name = "";
                    

                    if (!existingOperators.ContainsKey(id))
                    {
                        var refOperator = coreRefData.Operators.FindAll(i => i != null)
                            .Find(u => 
                                (!string.IsNullOrEmpty(name) && 
                                 !string.IsNullOrEmpty(u.Title) && 
                                 u.Title.ToLower().Contains(name.ToLower())) || 

                                (!string.IsNullOrEmpty(webpage) && 
                                 !string.IsNullOrEmpty(u.WebsiteURL) && 
                                 u.WebsiteURL.Contains(webpage)) || 

                                (!string.IsNullOrEmpty(u.Comments) && 
                                 u.Comments.Contains($"LKFI-ID:{id};"))
                            );

                        if (refOperator == null)
                        {
                            Log($"OPERATOR MISSING LATAUSKARTTAFI: LKFI-ID:{id}, {name}");
                        }
                        else
                        {
                            existingOperators.Add(id, refOperator.ID);
                        }
                    }
                }
            }
            
            /*
             * Meaning of items in latauskartta.fi API:
             * chargers -> EVSEs
             * locations -> Map point where multiple CPOs can be under
             * sublocations -> each CPO under the location
             */

            var allEvses = ((JArray)msg.GetValue("chargers")).ToObject<List<JObject>>();
            
            foreach (JObject chargerstation in ((JObject)msg.GetValue("locations")).Values().OfType<JObject>())
            {
                
                var sublocations = chargerstation.GetValue("sublocations")?.Values()?.OfType<JObject>()?.ToList() ?? [];

                foreach (JObject sublocation in sublocations)
                {
                    var evses = allEvses.FindAll(evse =>
                        (string)evse["location"] == (string)chargerstation["id"] &&
                        (string)evse["sublocation_id"] == (string)sublocation["sublocation_id"]);
                    
                    ChargePoint cp = new ChargePoint();
                    cp.DataProviderID = this.DataProviderID;
                    cp.DataProvidersReference = $"{(string)chargerstation["id"]}-{(string)sublocation["sublocation_id"]}";

                    cp.DateLastStatusUpdate = DateTime.UtcNow;
                    if (chargerstation.ContainsKey("updated") && (string)chargerstation["updated"] != null) 
                        cp.DateLastStatusUpdate = DateTime.UnixEpoch.AddSeconds(long.Parse((string)chargerstation["updated"]));
                    cp.AddressInfo = new AddressInfo();

                    cp.AddressInfo.Title = $"{(string)sublocation["title_en"] ?? (string)sublocation["title"]} {(string)chargerstation["title"] ?? ""}";

                    cp.AddressInfo.AddressLine1 = ((string)chargerstation["address"] ?? "").Split(",").FirstOrDefault().Trim();
                    cp.AddressInfo.Town = ((string)chargerstation["address"] ?? "").Split(",").LastOrDefault()?.Trim().Split(" ").LastOrDefault()?.Trim();
                    cp.AddressInfo.Postcode = ((string)chargerstation["address"] ?? "").Split(",").LastOrDefault()?.Trim().Split(" ").FirstOrDefault()?.Trim();

                    cp.AddressInfo.Latitude = double.Parse((string)sublocation["lat"], CultureInfo.InvariantCulture);
                    cp.AddressInfo.Longitude = double.Parse((string)sublocation["lon"], CultureInfo.InvariantCulture);

                    // Find country or Finland by default
                    cp.AddressInfo.CountryID = coreRefData.Countries.Find(country => country.ISOCode == (string)chargerstation["country"])?.ID ?? 79;

                    cp.AddressInfo.AccessComments =
                        (string)chargerstation["info_en"] ?? (string)chargerstation["info"];

                    cp.NumberOfPoints = evses.Count;
                    
                    // Paid
                    cp.UsageTypeID = 0;


                    int operatorId = 1;

                    if (evses.Count != 0)
                    {
                        var operatorIdStr = evses.Find(itm => itm.GetValue("operator") != null)?.GetValue("operator");
                        if (operatorIdStr != null && existingOperators.ContainsKey((string)operatorIdStr))
                        {
                            // free
                            cp.UsageTypeID = (string)operatorIdStr == "0" ? 1 : 4;
                            operatorId = existingOperators[(string)operatorIdStr];
                        }
                    }

                    cp.OperatorID = operatorId;
                    

                    foreach (JObject evse in evses)
                    {
                        ConnectionInfo cinfo = new ConnectionInfo() { };

                        cinfo.Reference = (string)evse["id"];
                        
                        cinfo.StatusTypeID = statusMap.GetValueOrDefault((string)evse["status"]);
                        cinfo.Quantity = int.Parse((string)evse["kpl"]);
                        cinfo.ConnectionTypeID = plugTypesMap.GetValueOrDefault((string)evse["type"]);
                        cinfo.PowerKW = double.Parse((string)evse["kw"]);
                        cinfo.Comments = (string)((string)evse.GetValue("info_en") ?? (string)evse.GetValue("info") ?? "");
                        cinfo.LevelID = cinfo.ConnectionTypeID is 35 or 25 ? 2 : cinfo.ConnectionTypeID != null ? 3 : null;
                        // Finland has 3 phase for AC charging
                        cinfo.CurrentTypeID = cinfo.ConnectionTypeID is 35 or 25 ? (int)StandardCurrentTypes.ThreePhaseAC : 
                            cinfo.ConnectionTypeID != null ? (int)StandardCurrentTypes.DC : null;
                        if (cinfo.Comments == null)
                        {
                            cinfo.Comments = "";
                        }
                        if (evse.ContainsKey("kw_800v") && (string)evse["kw_800v"] != null)
                            cinfo.Comments += $"\n\n Charging Power for 800V Cars: {(string)evse["kw_800v"]} kW\n\n";
                        if (evse.ContainsKey("kw_total") && (string)evse["kw_total"] != null)
                            cinfo.Comments += $"\n\n Total station power: {(string)evse["kw_total"]} kW\n\n";
                        
                        
                        
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
                }
                
            }

            return outputList;
        }
    }
}
