using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.Import.Providers
{
    public class ImportProvider_DataGouvFr : BaseImportProvider, IImportProvider
    {
        public ImportProvider_DataGouvFr()
        {
            ProviderName = "data.gouv.fr";
            OutputNamePrefix = "data.gouv.fr";
            AutoRefreshURL = "https://www.data.gouv.fr/api/1/datasets/?q=5448d3e0c751df01f85d0572&page=0&page_size=20";
            IsProductionReady = true;
            MergeDuplicatePOIEquipment = true;
            DataProviderID = 28;
        }

        private void TrackOccurrence(Dictionary<string, int> occurrences, string item)
        {
            if (occurrences.TryGetValue(item, out var count))
            {
                occurrences.Remove(item);
                occurrences.Add(item, count + 1);
            }
            else
            {
                occurrences.Add(item, 1);
            }
        }

        private void LogOccurences(Dictionary<string, int> occurrences, string title)
        {
            Log(title);
            foreach (var item in occurrences.OrderByDescending(o => o.Value))
            {
                Log($"{item.Key} : {item.Value}");
            }
        }

        public List<ChargePoint> Process(CoreReferenceData refData)
        {

#if DEBUG

            var poiDataCsv = System.IO.File.ReadAllText(@"C:\\Temp\\ocm\\data\\import\\cache_data.gouv.fr.dat");
#else
            JObject o = JObject.Parse(InputData);
            var resourceUrl = o["resources"][0]["latest"].ToString();

            HttpClient client = new HttpClient();
            var poiDataCsv= client.GetStringAsync(resourceUrl).Result;
#endif

            //whole file cleanup (broken newline in text)
            InputData = poiDataCsv.Replace("\n\";", "\";");

            var results = InputData.Split('\n');

            var preprocessedRows = new List<string>();
            // preprocess data to find line breaks in addresses and repair those rows
            for (var n = 1; n < results.Length; n++)
            {
                var row = results[n];

                if (n + 1 < results.Length)
                {
                    var nextRow = results[n + 1];

                    if (!string.IsNullOrEmpty(nextRow) && (Char.IsDigit(nextRow[0]) || row.Length < 200))
                    {
                        // probably a postcode or broken line in title, merge row with next row
                        row = row.Trim() + " " + nextRow;

                        Log("Repaired row " + n + "::" + row);
                        n++; // skip next row
                    }
                }

                preprocessedRows.Add(row);
            }

            var keyLookup = results[0].Replace("\r", "").Split(';').ToList();

            Dictionary<string, int> unmatchedNetworks = new Dictionary<string, int>();
            Dictionary<string, int> unmatchedUsage = new Dictionary<string, int>();
            Dictionary<string, int> unmatchedConnections = new Dictionary<string, int>();

            List<ChargePoint> outputList = new List<ChargePoint>();
            int rowIndex = 0;
            foreach (var row in preprocessedRows)
            {
                //skip first row or empty rows
                if (rowIndex > 0 && !String.IsNullOrEmpty(row))
                {
                    if (!(String.IsNullOrEmpty(row.Replace(";", "").Trim())))
                    {
                        try
                        {
                            var cols = row.Replace("\r", "").Replace(";", " ;").Split(';');
                            var poi = new ChargePoint();

                            var title = cols[keyLookup.FindIndex(a => a == "n_station")]?.Trim();
                            var usageType = cols[keyLookup.FindIndex(a => a == "acces_recharge")];
                            var operatorName = cols[keyLookup.FindIndex(a => a == "n_operateur")]?.ToLower().Trim();
                            var powerKw = cols[keyLookup.FindIndex(a => a == "puiss_max")].Trim()
                                .Replace("kw", "", StringComparison.InvariantCultureIgnoreCase)
                                .Replace("kva", "", StringComparison.InvariantCultureIgnoreCase);

                            var connectionType = cols[keyLookup.FindIndex(a => a == "type_prise")];
                            var latitude = cols[keyLookup.FindIndex(a => a == "Ylatitude")].Replace("Ê", "").Replace("\"", "").Replace("*", ".");
                            var longitude = cols[keyLookup.FindIndex(a => a == "Xlongitude")].Replace("Ê", "").Replace("\"", "").Replace("*", ".");
                            var reference = cols[keyLookup.FindIndex(a => a == "id_station")]?.Trim();
                            var address = cols[keyLookup.FindIndex(a => a == "ad_station")]?.Trim();
                            var numStations = cols[keyLookup.FindIndex(a => a == "nbre_pdc")];
                            var hours = cols[keyLookup.FindIndex(a => a == "accessibilité")];
                            var additionalInfo = cols[keyLookup.FindIndex(a => a == "observations")];
                            var networkName = cols[keyLookup.FindIndex(a => a == "n_enseigne")]?.ToLower().Trim();

                            poi.DataProviderID = DataProviderID;
                            poi.DataProvidersReference = reference?.Trim();
                            poi.AddressInfo = new AddressInfo();

                            poi.AddressInfo.Title = title?.Trim();
                            poi.AddressInfo.AddressLine1 = address.Trim();

                            poi.AddressInfo.Latitude = double.Parse(latitude);
                            poi.AddressInfo.Longitude = double.Parse(longitude);

                            poi.AddressInfo.AccessComments = hours?.Trim();
                            poi.AddressInfo.CountryID = 80; // assume France

                            poi.GeneralComments = additionalInfo?.Trim();

                            //TODO: Operator and Operators Reference
                            // Actually, Operators Reference is the reference field, it's in the standard OCPP/OCHP format provider's reference should perhaps be the 'source' field

                            if (usageType.Contains("payant", StringComparison.InvariantCultureIgnoreCase)) usageType = "payant";

                            //resolve usage type
                            switch (usageType.ToLower().Trim())
                            {
                                case "payant":
                                    poi.UsageTypeID = (int)StandardUsageTypes.Public_PayAtLocation;
                                    break;

                                case "gratuit":
                                    poi.UsageTypeID = (int)StandardUsageTypes.Public_PayAtLocation;
                                    break;
                                default:
                                    Log("Unknown Usage Type: " + usageType);

                                    TrackOccurrence(unmatchedUsage, usageType);
                                    break;
                            }

                            //parse equipment info
                            var stations = numStations.Split('+');
                            if (String.IsNullOrWhiteSpace(numStations)) stations = new string[] { "1" }; //blank, default to 1
                            var connectionTypes = connectionType.Replace("-", "+").Split('+');

                            foreach (var c in connectionTypes)
                            {
                                if (poi.Connections == null) poi.Connections = new List<ConnectionInfo>();

                                var connection = new ConnectionInfo();

                                var cType = c.ToLower().Trim();
                                if (cType.Contains("cahdemo") || cType.Contains("chademo")) cType = "chademo";


                                switch (cType)
                                {
                                    case "ef":
                                    case "e/f":
                                        connection.ConnectionTypeID = (int)StandardConnectionTypes.Schuko;
                                        connection.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                                        break;

                                    case "mennekes":
                                    case "mennekes.m":
                                    case "t2":
                                    case "type 2":
                                        connection.ConnectionTypeID = (int)StandardConnectionTypes.MennekesType2;
                                        connection.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                                        break;

                                    case "schuko":
                                    case "mennekes.f":
                                        connection.ConnectionTypeID = (int)StandardConnectionTypes.Schuko;
                                        connection.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                                        break;

                                    case "ccs combo2":
                                    case "combo 2":
                                    case "combo2":
                                    case "combo":
                                        connection.ConnectionTypeID = (int)StandardConnectionTypes.CCSComboType2;
                                        connection.CurrentTypeID = (int)StandardCurrentTypes.DC;
                                        break;
                                    case "ccs350":
                                        connection.ConnectionTypeID = (int)StandardConnectionTypes.CCSComboType2;
                                        connection.PowerKW = 350;
                                        break;
                                    case "ccs50":
                                        connection.ConnectionTypeID = (int)StandardConnectionTypes.CCSComboType2;
                                        connection.PowerKW = 50;
                                        break;
                                    case "t3":
                                    case "type 3":
                                        connection.ConnectionTypeID = (int)StandardConnectionTypes.Type3;
                                        break;
                                    case "j1772":
                                        connection.ConnectionTypeID = (int)StandardConnectionTypes.J1772;
                                        connection.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                                        break;
                                    case "chademo":
                                        connection.ConnectionTypeID = (int)StandardConnectionTypes.CHAdeMO;
                                        connection.CurrentTypeID = (int)StandardCurrentTypes.DC;
                                        break;
                                    default:
                                        if (cType.Contains("t2"))
                                        {
                                            connection.ConnectionTypeID = (int)StandardConnectionTypes.MennekesType2;
                                            connection.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                                        }
                                        else if (cType.Contains("t3"))
                                        {
                                            connection.ConnectionTypeID = (int)StandardConnectionTypes.Type3;

                                        }
                                        else
                                        {
                                            Log("Unknown Connection Type: " + cType);
                                            TrackOccurrence(unmatchedConnections, cType);
                                        }
                                        break;
                                }

                                double.TryParse(powerKw, out var parsedKw);
                                if (parsedKw > 0)
                                {
                                    connection.PowerKW = parsedKw;
                                    if ((connection.PowerKW > 7.5) && (connection.CurrentTypeID == (int)StandardCurrentTypes.SinglePhaseAC) && (connection.ConnectionTypeID == (int)StandardConnectionTypes.MennekesType2))
                                    {
                                        connection.CurrentTypeID = (int)StandardCurrentTypes.ThreePhaseAC;
                                    }

                                    if ((connection.PowerKW >= 22) && (connection.ConnectionTypeID == (int)StandardConnectionTypes.MennekesType2))
                                    {
                                        // Type 2 connectors aren't allowed to be socketed for >32A
                                        connection.ConnectionTypeID = (int)StandardConnectionTypes.MennekesType2Tethered;
                                    }
                                }
                                else
                                {
                                    if (powerKw != "0")
                                    {
                                        Log("Unknown Power: " + powerKw);
                                    }
                                }

                                connection.StatusTypeID = (int)StandardStatusTypes.Operational;
                                poi.StatusTypeID = (int)StandardStatusTypes.Operational;


                                poi.Connections.Add(connection);
                            }


                            if (poi.DataQualityLevel == null) poi.DataQualityLevel = 2;

                            poi.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Imported_Published;

                            if (!string.IsNullOrWhiteSpace(networkName))
                            {
                                // specific network mappings
                                if (networkName == "e born") networkName = "eborn";
                                if (networkName == "morbihan énergies") networkName = "freshmile";

                                // shared network mappings
                                if (operatorName == "freshmile") networkName = "freshmile";

                                var network = refData.Operators.FirstOrDefault(o => o.Title.Contains(networkName, StringComparison.InvariantCultureIgnoreCase));

                                if (network == null)
                                {
                                    TrackOccurrence(unmatchedNetworks, networkName);
                                }
                                else
                                {
                                    poi.OperatorID = network.ID;
                                }
                            }

                            outputList.Add(poi);
                        }
                        catch (Exception exp)
                        {
                            Log("Failed to parse row: " + rowIndex + " " + exp.ToString() + " :: " + row);
                        }
                    }
                }
                rowIndex++;
            }

            LogOccurences(unmatchedUsage, "Unmatched Usage Types");

            LogOccurences(unmatchedNetworks, "Unmatched Networks");

            LogOccurences(unmatchedConnections, "Unmatched Connections");
            return outputList;
        }
    }
}
