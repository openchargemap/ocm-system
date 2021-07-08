using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCM.Import.Providers
{
    public class ImportProvider_Bundesnetzagentur : BaseImportProvider, IImportProvider
    {
        public ImportProvider_Bundesnetzagentur()
        {
            ProviderName = "Bundesnetzagentur.de";
            OutputNamePrefix = "bundesnetzagentur";
            AutoRefreshURL = "https://services6.arcgis.com/6jU7RmJig2Wwo1b0/arcgis/rest/services/Ladesaeulenregister/FeatureServer/7/query?f=json&spatialRel=esriSpatialRelIntersects&geometry=%7B%22xmin%22%3A5.0986681876%2C%22ymin%22%3A47.0416812506%2C%22xmax%22%3A15.6601907311%2C%22ymax%22%3A55.1299067491%2C%22spatialReference%22%3A%7B%22wkid%22%3A4326%7D%7D&geometryType=esriGeometryEnvelope&inSR=25832&outFields=*&returnCentroid=false&returnExceededLimitFeatures=false&maxRecordCountFactor=3&outSR=25832&resultType=tile";
            IsAutoRefreshed = true;
            IsProductionReady = true;
            MergeDuplicatePOIEquipment = false;
            DataAttribution = "Data from the German chargepoint registry provided by Bundesnetzagentur.de under the CC-BY 4.0 license";
            DataProviderID = 29;//Bundesnetzagentur
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
            List<ChargePoint> outputList = new List<ChargePoint>();

            string source = InputData;

            JObject o = JObject.Parse(source);

            var dataList = o["features"].ToArray();

            int itemCount = 0;

            // address, operator, chargepoint
            var dataMap = new Dictionary<Tuple<String, int?>, ChargePoint>();

            var unknownOperators = new Dictionary<String, int>();

            foreach (var dataItem in dataList)
            {
                var item = dataItem["attributes"];

                var cp = new POIDetails();

                cp.DataProviderID = this.DataProviderID; //Bundesnetzagentur
                cp.DataProvidersReference = item["OBJECTID"].ToString();
                cp.DateLastStatusUpdate = DateTime.UtcNow;

                cp.AddressInfo = new AddressInfo();

                var locationStr = item["Standort_"].ToString();
                var address = locationStr.Split(", ");
                cp.AddressInfo.AddressLine1 = address[0];
                cp.AddressInfo.Title = address[0];

                var postcodeAndTown = address[1].Split(" ");
                cp.AddressInfo.Postcode = postcodeAndTown[0];
                cp.AddressInfo.Town = String.Join(" ", postcodeAndTown[1..]);

                cp.AddressInfo.Latitude = (double) item["Breitengrad_"];
                cp.AddressInfo.Longitude = (double) item["Längengrad_"];

                // all chargers in this directory are in Germany (ID 87)
                cp.AddressInfo.Country = cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(cy => cy.ID == 87);

                var operatorName = item["Betreiber_"].ToString();
                cp.OperatorID = coreRefData.Operators.FirstOrDefault(devOp => devOp.Title.ToLower().Contains(operatorName.ToLower()))?.ID;

                if (cp.OperatorID == null)
                {
                    switch (operatorName)
                    {
                        case "EnBW Energie Baden-Württemberg":
                        case "EnBW Ostwürttemberg DonauRies AG":
                            cp.OperatorID = 86;  // EnBW (D)
                            break;
                        case "Allego GmbH":
                            cp.OperatorID = 103; // Allego BV
                            break;
                        case "ubitricity Gesellschaft für verteilte Energiesysteme mbH":
                            cp.OperatorID = 2244;  // ubitricity
                            break;
                        case "EWE Go GmbH":
                        case "EWE VERTRIEB GmbH":
                            cp.OperatorID = 127;  // EWE
                            break;
                        case "EAM Netz GmbH":
                            cp.OperatorID = 140;  // EAM
                            break;
                        case "VW Group Charging GmbH":
                            cp.OperatorID = 3437;  // Elli (Volkswagen Group Charging GmbH)
                            break;
                        case "innogy eMobility Solutions GmbH":
                            cp.OperatorID = 105;  // Innogy SE (RWE eMobility)
                            break;
                        case "eins energie in sachsen GmbH & Co. KG":
                            cp.OperatorID = 2243;  // eins
                            break;
                        case "Q1 Energie AG":
                            cp.OperatorID = 3429;  // Q1 Autostrom
                            break;
                        case "Energiedienst Holding AG":
                            cp.OperatorID = 224;  // EnergieDienst
                            break;
                        case "Oberhessische Versorgungsbetriebe AG":
                            cp.OperatorID = 188;  // OVAG Energie
                            break;
                        case "Energie- und Wasserversorgung Bruchsal GmbH":
                            cp.OperatorID = 2241;  // EWB
                            break;
                        case "BIGGE ENERGIE GmbH & Co. KG":
                            cp.OperatorID = 3443;  // BIGGIE Energie -> Typo!
                            break;
                        case "EVH GmbH":
                            cp.OperatorID = 161;  // Stadtwerke Halle
                            break;
                        case "SWS Energie GmbH":
                            cp.OperatorID = 153;  // Stadtwerke Strahlsund -> Typo! (should be Stadtwerke Stralsund)
                            break;
                        case "Entega Plus GmbH":
                            cp.OperatorID = 3297;  // Entega
                            break;
                        case "Energieversorgung Limburg GmbH":
                            cp.OperatorID = 3351;  // EVL (de)
                            break;
                        case "SWM Versorgungs GmbH":
                        case "Vereinigte Stadtwerke GmbH":
                        case "Stadtwerke Stade GmbH":
                        case "Stadtwerke Villingen-Schwenningen GmbH":
                        case "Gemeindewerke Murnau":
                        case "Stadtwerke Fürstenfeldbruck GmbH":
                        case "Stadtwerke Plattling":
                        case "Stadtwerke Bad Langensalza GmbH":
                        case "Stadtwerke Weimar Stadtversorgungs-GmbH":
                        case "Stadtwerke Meiningen":
                        case "Energieversorgung Nordhausen GmbH":
                        case "Stadtwerke Ilmenau GmbH":
                        case "Stadtwerke Duisburg AG":
                        case "Filderstadtwerke":
                        case "Osanabrücker Parkstätten-Betriebsgesellschaft mbH":
                        case "SWU Energie GmbH":  // also listed separately as "Schwabencard", ID 2245
                        case "Stadtwerke Augsburg Energie GmbH":
                        case "FairEnergie GmbH":
                        case "Energie Waldeck-Frankenberg GmbH":
                        case "ewerk Sachsenwald GmbH":
                        case "Stadtwerke Tecklenburger Land GmbH & Co. KG":
                        case "SWE Energie GmbH":
                        case "WSW Energie & Wasser AG":
                        case "Albwerk GmbH & Co. KG":
                        case "EVI Energieversorgung Hildesheim GmbH & Co. KG":
                        case "Stadtwerke Energie Jena-Pößneck GmbH":
                        case "EMB Energie Mark Brandenburg GmbH":
                        case "StWB Stadtwerke Brandenburg an der Havel GmbH & Co.KG":
                        case "SWT Parken GmbH":
                        case "Stadtwerke Neuruppin GmbH":
                        case "badenova AG & Co. KG":  // also listed separately as "Badenova (DE)", ID 3292
                        case "Thüga Energie GmbH":
                        case "Gesellschaft des Kreises Coesfeld zur Förderung regenerativer Energien mbH":
                        case "nvb Nordhorner Versorgungsbetriebe GmbH":
                        case "WEMAG AG":
                        case "Energieversorgung Mittelrhein AG":
                        case "Stadtwerke Aalen GmbH":
                        case "Überlandwerk Erding GmbH & Co. KG":
                        case "Stadtwerke Heilbronn GmbH":
                        case "Stadtwerke Nürtingen GmbH":
                        case "Stadtwerke Borken/Westf. GmbH":
                        case "Stadtwerke Neuss Energie und Wasser GmbH":
                        case "Stadtwerke Emden GmbH":
                        case "Energie Südbayern GmbH":
                        case "Aschaffenburger Versorgungs GmbH":
                        case "Stadtwerke Oberkirch GmbH":
                        case "Wasserwerk Vechta - Eigenbetrieb Stadt Vechta":
                        case "Stadtwerke Radolfzell GmbH":
                        case "Stadtwerke Forst GmbH":
                        case "Stadtwerke Springe GmbH":
                        case "Stadtwerke Velbert GmbH":
                        case "Eisenacher Versorgungs-Betriebe GmbH":
                        case "Stadtwerke Coesfeld GmbH":
                        case "SWK Stadtwerke Kaiserslautern Versorgungs-AG":
                        case "Stadtwerke Suhl/Zella-Mehlis GmbH":
                        case "Stadtwerke Lünen GmbH":
                        case "Stadtwerke Mosbach GmbH":
                        case "enwor - energie & wasser vor ort GmbH":
                            // these and some other local municipal utilities are all part of Ladenetz, see list at https://ladenetz.de/community/
                            cp.OperatorID = 69;  // ladenetz.de
                            break;
                        case "Stadtwerke Norderstedt (Städtischer Eigenbetrieb)":
                            // Stadtwerke Norderstedt are part of the charging network of Stromnetz Hamburg
                            cp.OperatorID = 220;  // Stromnetz Hamburg
                            break;
                        case "Privatperson":
                            cp.OperatorID = 44;  // Private individual
                            break;
                        case "ALDI SE & Co. KG":
                        case "ALDI GmbH & Co KG":
                        case "ALDI GmbH & Co. KG":
                        case "Lidl Dienstleistung GmbH & Co. KG":
                        case "Kaufland Dienstleistung GmbH & Co. KG":
                        case "IKEA Deutschland GmbH":
                        case "EDEKA Versorgungsgesellschaft mbH":
                        case "Flughafen Stuttgart GmbH":
                        case "Schubert Motors GmbH":
                            // Business Owner at location
                            cp.OperatorID = 45;
                            break;
                    }
                }

                if (cp.OperatorID == null)
                {
                    // try stripping legal entity types like AG, GmbH, eG, GmbH & Co. KG and other common suffixes
                    var operatorNameStripped = operatorName
                        .Replace(" AG", "").Replace(" GmbH & Co. KG", "").Replace(" GmbH & Co KG", "").Replace(" GmbH", "").Replace(" eG", "").Replace(" Aktiengesellschaft", "")
                        .Replace(" Energie", "").Replace(" Deutschland", "")
                        .Trim();
                    cp.OperatorID = coreRefData.Operators.FirstOrDefault(devOp => devOp.Title.ToLower().Contains(operatorNameStripped.ToLower()))?.ID;
                }

                if (cp.OperatorID == null)
                {
                    if (operatorName.Contains("Autohaus") || operatorName.Contains("Niederlassung") || operatorName.ToLower().Contains("hotel"))
                    {
                        // car dealerships
                        cp.OperatorID = 45;  // Business Owner at location
                    }
                }

                if (cp.OperatorID == null)
                {
                    //Log("Unknown Operator: " + operatorName);
                    if (unknownOperators.ContainsKey(operatorName))
                    {
                        unknownOperators[operatorName]++;
                    }
                    else
                    {
                        unknownOperators.Add(operatorName, 1);
                    }
                }


                var plugCount = (int) item["Anzahl_Ladepunkte_"];
                cp.Connections = new List<ConnectionInfo>();
                for (var i = 1; i <= plugCount; i++)
                {
                    var plugs = new List<int>();
                    var currentType = null as int?;
                    var power = Double.Parse(item["Nennleistung_Ladepunkt_" + i + "_"].ToString().Split(" ")[0]);

                    if (item["Sonstige_Stecker__" + i + "_"].ToString().Length > 0)
                    {
                        var plug = item["Sonstige_Stecker__" + i + "_"].ToString();
                        if (plug == "Steckdose Typ 1")
                        {
                            plugs.Add((int)StandardConnectionTypes.J1772);
                            currentType = (int)StandardCurrentTypes.SinglePhaseAC;
                        }
                        else
                        {
                            Log("Unknown Plug: " + item["Sonstige_Stecker__" + i + "_"].ToString());
                        }
                    }
                    else if (item["AC_Schuko__" + i + "_"].ToString().Length > 0)
                    {
                        plugs.Add((int)StandardConnectionTypes.Schuko);
                        currentType = (int)StandardCurrentTypes.SinglePhaseAC;
                    }
                    else if (item["AC_CEE_5_polig__" + i + "_"].ToString().Length > 0)
                    {
                        plugs.Add(17); // CEE 5 Pin
                        currentType = (int)StandardCurrentTypes.ThreePhaseAC;
                    }
                    else if (item["AC_CEE_3_polig__" + i + "_"].ToString().Length > 0)
                    {
                        plugs.Add(16); // CEE 3 Pin
                        currentType = (int)StandardCurrentTypes.SinglePhaseAC;
                    }
                    else if (item["AC_Steckdose_Typ_2__" + i + "_"].ToString().Length > 0)
                    {
                        plugs.Add((int) StandardConnectionTypes.MennekesType2);
                        currentType = power >= 11 ? (int) StandardCurrentTypes.ThreePhaseAC : (int) StandardCurrentTypes.SinglePhaseAC;
                    } 
                    else if (item["AC_Kupplung_Typ_2__" + i + "_"].ToString().Length > 0)
                    {
                        plugs.Add((int)StandardConnectionTypes.MennekesType2Tethered);
                        currentType = power >= 11 ? (int)StandardCurrentTypes.ThreePhaseAC : (int)StandardCurrentTypes.SinglePhaseAC;
                    }
                    else if (item["DC_Kupplung_Combo__" + i + "_"].ToString().Length > 0)
                    {
                        plugs.Add((int)StandardConnectionTypes.CCSComboType2);
                        currentType = (int)StandardCurrentTypes.DC;
                    }
                    else if (item["DC_CHAdeMO__" + i + "_"].ToString().Length > 0)
                    {
                        plugs.Add((int)StandardConnectionTypes.CHAdeMO);
                        currentType = (int)StandardCurrentTypes.DC;
                    }

                    foreach (var plug in plugs)
                    {
                        ConnectionInfo cinfo = new ConnectionInfo() { };
                        cinfo.PowerKW = power;
                        cinfo.ConnectionTypeID = plug;
                        cinfo.CurrentTypeID = currentType;
                        cp.Connections.Add(cinfo);
                    }
                }

                //apply data attribution metadata
                if (cp.MetadataValues == null) cp.MetadataValues = new List<MetadataValue>();
                cp.MetadataValues.Add(new MetadataValue { MetadataFieldID = (int)StandardMetadataFields.Attribution, ItemValue = DataAttribution });

                if (cp.DataQualityLevel == null) cp.DataQualityLevel = 3;

                if (cp.SubmissionStatusTypeID == null) cp.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Imported_Published;

                // add or merge with existing
                var key = new Tuple<String, int?>(locationStr, cp.OperatorID);
                if (dataMap.ContainsKey(key))
                {
                    var matchingCP = dataMap[key];
                    matchingCP.Connections.AddRange(cp.Connections);
                    matchingCP.DataProvidersReference = matchingCP.DataProvidersReference + "," + cp.DataProvidersReference;
                } else
                {
                    dataMap.Add(key, new ChargePoint(cp));
                }
            }

            Log("Unknown Operators from Bundesnetzagentur import:");
            foreach (var item in unknownOperators.OrderByDescending(item => item.Value))
            {
                Log(item.Key + ": " + item.Value);
            }

            return dataMap.Values.ToList();
        }
    }
}
