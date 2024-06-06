
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.Import.Providers
{
    public class ImportProvider_UKChargePointRegistry : BaseImportProvider, IImportProvider
    {
        public ImportProvider_UKChargePointRegistry()
        {
            ProviderName = "chargepoints.dft.gov.uk";
            OutputNamePrefix = "ukchargepointregistry";
            AutoRefreshURL = "https://chargepoints.dft.gov.uk/api/retrieve/registry/format/json";
            IsAutoRefreshed = true;
            IsProductionReady = true;
            MergeDuplicatePOIEquipment = false;
            DataAttribution = "Contains public sector information licensed under the Open Government Licence v2.0. http://www.nationalarchives.gov.uk/doc/open-government-licence/version/2/";
            DataProviderID = 18;//UK Chargepoint Registry
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
            var outputList = new List<ChargePoint>();

            var source = InputData;

            var o = JObject.Parse(source);

            //System.IO.File.WriteAllText("c:\\temp\\ukchargepointregistry.json", JsonConvert.SerializeObject(o, Formatting.Indented));

            var dataList = o["ChargeDevice"].ToArray();

            var itemCount = 0;

            var unmappedOperators = new Dictionary<string, int>();

            foreach (var dataItem in dataList)
            {
                var skipPOI = false;
                var item = dataItem;
                var cp = new POIDetails();

                var deviceName = item["ChargeDeviceName"].ToString();

                //private addresses are skipped from import
                if (!String.IsNullOrEmpty(deviceName) && deviceName.ToLower().Contains("parkatmyhouse"))
                {
                    skipPOI = true;
                }
                else
                {

                    var locationType = item["LocationType"].ToString();
                    if (!String.IsNullOrEmpty(locationType))
                    {
                        if (locationType.ToLower().Contains("home"))
                        {
                            skipPOI = true;
                        }
                    }

                    //parse reset of POI data
                    cp.DataProviderID = this.DataProviderID; //UK National Charge Point Registry
                    cp.DataProvidersReference = item["ChargeDeviceId"].ToString();
                    cp.DateLastStatusUpdate = DateTime.UtcNow;
                    cp.AddressInfo = new AddressInfo();

                    var locationDetails = item["ChargeDeviceLocation"];
                    var addressDetails = locationDetails["Address"];

                    cp.AddressInfo.RelatedURL = "";
                    cp.DateLastStatusUpdate = DateTime.UtcNow;


                    var street = addressDetails["Street"]?.ToString().Trim();
                    var thoroughfare = addressDetails["Thoroughfare"]?.ToString().Trim();
                    var buildingNumber = addressDetails["BuildingNumber"]?.ToString().Trim();
                    var buildingName = addressDetails["BuildingName"]?.ToString().Trim();

                    cp.AddressInfo.AddressLine1 = street?.Replace("<br>", ", ").Trim();

                    if (cp.AddressInfo.AddressLine1?.ToLower().StartsWith("asset no.") == true || cp.AddressInfo.AddressLine1?.ToLower() == "na")
                    {
                        cp.AddressInfo.AddressLine1 = "";
                    }

                    if (string.IsNullOrEmpty(cp.AddressInfo.AddressLine1))
                    {
                        if (!string.IsNullOrEmpty(buildingNumber) && !string.IsNullOrEmpty(thoroughfare))
                        {
                            cp.AddressInfo.AddressLine1 = $"{buildingNumber} {thoroughfare}";
                        }
                        else if (!string.IsNullOrEmpty(buildingName))
                        {
                            cp.AddressInfo.AddressLine1 = buildingName;
                        }
                        else if (!string.IsNullOrEmpty(thoroughfare))
                        {
                            cp.AddressInfo.AddressLine1 = thoroughfare;
                        }
                    }

                    if (string.IsNullOrEmpty(cp.AddressInfo.AddressLine1))
                    {
                        Log("Could not determine address line 1");
                    }

                    cp.AddressInfo.Town = addressDetails["PostTown"].ToString();
                    var dependantLocality = addressDetails["DependantLocality"].ToString();
                    if (!String.IsNullOrEmpty(dependantLocality) && dependantLocality.ToLower() != cp.AddressInfo.Town.ToLower())
                    {
                        //use dependantLocality if provided and is not same as town
                        cp.AddressInfo.AddressLine2 = dependantLocality;
                    }
                    cp.AddressInfo.Postcode = addressDetails["PostCode"].ToString().Trim();
                    cp.AddressInfo.Latitude = double.Parse(locationDetails["Latitude"].ToString());
                    cp.AddressInfo.Longitude = double.Parse(locationDetails["Longitude"].ToString());
                    cp.AddressInfo.AccessComments = locationDetails["LocationLongDescription"].ToString().Replace("<br>", ", ").Replace("\r\n", ", ").Replace("\n", ", ");

                    // try to determine a suitable title for the location

                    var chargeDeviceName = item["ChargeDeviceName"]?.ToString().Trim();
                    var chargeDeviceText = item["ChargeDeviceText"]?.ToString().Trim();
                    var chargeDeviceRef = item["ChargeDeviceRef"]?.ToString().Trim();
                    var locationShortDescription = locationDetails["LocationShortDescription"]?.ToString().Trim();

                    // skip device name values that are typically not useful
                    if (chargeDeviceName.ToLower().StartsWith("suby") || chargeDeviceName.ToLower().StartsWith("steob"))
                    {
                        chargeDeviceName = "";
                    }
                    if (chargeDeviceText.ToLower().StartsWith("suby") || chargeDeviceName.ToLower().StartsWith("steob") || chargeDeviceText?.ToLower().StartsWith("public access") == true)
                    {
                        chargeDeviceText = "";
                    }

                    if (!string.IsNullOrEmpty(chargeDeviceText))
                    {
                        cp.AddressInfo.Title = chargeDeviceText;
                    }
                    else if (!string.IsNullOrEmpty(chargeDeviceName))
                    {
                        cp.AddressInfo.Title = chargeDeviceName;
                    }
                    else if (!string.IsNullOrEmpty(locationShortDescription))
                    {
                        cp.AddressInfo.Title = locationShortDescription;
                    }
                    else if (!string.IsNullOrEmpty(street) && !street.ToLower().Contains("asset no."))
                    {
                        cp.AddressInfo.Title = street;
                    }
                    else if (!string.IsNullOrEmpty(thoroughfare))
                    {
                        cp.AddressInfo.Title = thoroughfare;
                    }
                    else if (!string.IsNullOrEmpty(chargeDeviceRef))
                    {
                        cp.AddressInfo.Title = chargeDeviceRef;
                    }
                    else
                    {
                        // fail
                        cp.AddressInfo.Title = "";
                    }

                    cp.AddressInfo.Title = cp.AddressInfo.Title.Replace("&amp;", "&");
                    cp.AddressInfo.Title = cp.AddressInfo.Title.Replace("<br>", ", ").Trim();
                    if (cp.AddressInfo.Title.Length > 100) cp.AddressInfo.Title = cp.AddressInfo.Title.Substring(0, 64) + "..";

                    if (cp.AddressInfo.Title == "NA" && cp.AddressInfo.AddressLine1 == "NA")
                    {
                        // item needs an address resolved
                        cp.AddressInfo.Title = "";
                        cp.AddressCleaningRequired = true;
                        cp.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Imported_UnderReview;
                    }

                    if (cp.AddressInfo.Title == "NA")
                    {
                        cp.AddressInfo.Title = cp.AddressInfo.Postcode;
                    }

                    if (string.IsNullOrEmpty(cp.AddressInfo.Title))
                    {
                        Log("Could not identify address title");
                    }
                    //cp.AddressInfo.ContactTelephone1 = item["phone"].ToString();

                    if (!String.IsNullOrEmpty(addressDetails["Country"].ToString()))
                    {
                        var country = addressDetails["Country"].ToString();
                        int? countryID = null;

                        var countryVal = coreRefData.Countries.FirstOrDefault(c => c.Title.ToLower() == country.Trim().ToLower());
                        if (countryVal == null)
                        {
                            country = country.ToUpper();
                            //match country
                            if (country == "GB" || country == "US" || country == "USA" || country == "U.S." || country == "U.S.A.") countryID = 2;
                            if (country == "UK" || country == "GB" || country == "GREAT BRITAIN" || country == "UNITED KINGDOM") countryID = 1;
                        }
                        else
                        {
                            countryID = countryVal.ID;
                        }

                        if (countryID == null)
                        {
                            this.Log($"Country Not Matched, will require Geolocation: {addressDetails["Country"]}");
                        }
                        else
                        {
                            cp.AddressInfo.CountryID = countryID;
                        }
                    }

                    //operator from DeviceController
                    var deviceController = item["DeviceController"];
                    cp.AddressInfo.RelatedURL = deviceController["Website"].ToString();

                    var deviceControllerName = deviceController["OrganisationName"]?.ToString();
                    var deviceOwnerName = item["DeviceOwner"]["OrganisationName"]?.ToString();
                    var deviceNetworks = item["DeviceNetworks"]?.ToString().Split(",");

                    var networkOperator = deviceControllerName;

                    if (string.IsNullOrEmpty(networkOperator))
                    {
                        if (!string.IsNullOrEmpty(deviceOwnerName))
                        {
                            networkOperator = deviceOwnerName;
                        }
                        else if (deviceNetworks != null && deviceNetworks.Any())
                        {
                            networkOperator = deviceNetworks[0];
                        }
                    }

                    // match specific operators

                    switch (networkOperator?.ToLower().Trim())
                    {
                        case "alfa power":
                            cp.OperatorID = 3326;
                            break;
                        case "allego":
                            cp.OperatorID = 103;
                            break;
                        case "apt":
                            cp.OperatorID = 3341;
                            break;
                        case "be.ev":
                            cp.OperatorID = 3481;
                            break;
                        case "liberty charge":
                        case "believ":
                            cp.OperatorID = 3511;
                            break;
                        case "blink charging":
                            cp.OperatorID = 3737;
                            break;
                        case "bp-pulse (polar)":
                        case "bp pulse":
                            cp.OperatorID = 32;
                            break;
                        case "city ev":
                            cp.OperatorID = 3349;
                            break;
                        case "citipark":
                            cp.OperatorID = 3670;
                            break;
                        case "chargemaster (polar)":
                            cp.OperatorID = 8;
                            break;
                        case "charge your car":
                            cp.OperatorID = 20;
                            break;
                        case "chargeplace scotland":
                            cp.OperatorID = 3315;
                            break;
                        case "char.gy":
                            cp.OperatorID = 3345;
                            break;
                        case "chargepoint network (netherlands) b.v.":
                            cp.OperatorID = 5;
                            break;
                        case "clenergy ev":
                            cp.OperatorID = 3605;
                            break;
                        case "connected kerb":
                            cp.OperatorID = 3509;
                            break;
                        case "drax energy solutions limited":
                            cp.OperatorID = 3614;
                            break;
                        case "ecotricity (electric highway)":
                            cp.OperatorID = 24;
                            break;
                        case "ecar ni":
                            cp.OperatorID = 91;
                            break;
                        case "e.on drive":
                            cp.OperatorID = 3403;
                            break;
                        case "eb charging":
                            cp.OperatorID = 3391;
                            break;
                        case "esb ev solutions":
                            cp.OperatorID = 3357;
                            break;
                        case "elektromotive":
                            cp.OperatorID = 9;
                            break;
                        case "eo charging":
                            cp.OperatorID = 3298;
                            break;
                        case "ev-dot":
                            cp.OperatorID = 3446;
                            break;
                        case "ecars esb":
                            cp.OperatorID = 22;
                            break;
                        case "electroad":
                            cp.OperatorID = 3740;
                            break;
                        case "equans ev solutions":
                            cp.OperatorID = 150;
                            break;
                        case "ez-charge":
                            cp.OperatorID = 3515;
                            break;
                        case "fastned":
                            cp.OperatorID = 74;
                            break;
                        case "flowbird smart city uk ltd":
                            cp.OperatorID = 3739;
                            break;
                        case "gridserve sustainable energy":
                            cp.OperatorID = 3430;
                            break;
                        case "hubsta":
                            cp.OperatorID = 3356;
                            break;
                        case "instavolt ltd":
                            cp.OperatorID = 3296;
                            break;
                        case "joju ltd":
                            cp.OperatorID = 3393;
                            break;
                        case "mer":
                            cp.OperatorID = 3492;
                            break;
                        case "nissan":
                            cp.OperatorID = 7;
                            break;
                        case "osprey":
                            cp.OperatorID = 203;
                            break;
                        case "plug n go ltd":
                            cp.OperatorID = 3613;
                            break;
                        case "pod point":
                            cp.OperatorID = 3;
                            break;
                        case "project ev":
                            cp.OperatorID = 3629;
                            break;
                        case "ionity gmbh":
                            cp.OperatorID = 3299;
                            break;
                        case "surecharge/fm conway":
                            cp.OperatorID = 3612;
                            break;
                        case "scottishpower":
                            cp.OperatorID = 3537;
                            break;
                        case "shell recharge":
                        case "shell recharge solutions":
                        case "ubitricity":
                            cp.OperatorID = 3392;
                            break;
                        case "silverstone green energy":
                            cp.OperatorID = 3615;
                            break;
                        case "source london":
                            cp.OperatorID = 25;
                            break;
                        case "swarco e.connect":
                            cp.OperatorID = 3341;
                            break;
                        case "tonik energy":
                            cp.OperatorID = 3366;
                            break;
                        case "trojan energy limited":
                            cp.OperatorID = 3738;
                            break;
                        case "vendelectric":
                            cp.OperatorID = 3473;
                            break;
                        case "wattif ev uk limited":
                            cp.OperatorID = 3667;
                            break;
                        case "wenea services uk limited":
                            cp.OperatorID = 3371;
                            break;
                        case "zest":
                            cp.OperatorID = 3581;
                            break;
                    }

                    if (cp.OperatorID == null)
                    {
                        var unmappedOperator = networkOperator;
                        if (unmappedOperators.ContainsKey(unmappedOperator))
                        {
                            unmappedOperators[unmappedOperator]++;
                        }
                        else
                        {
                            unmappedOperators.Add(unmappedOperator, 1);
                        }
                    }

                    //determine most likely usage type
                    cp.UsageTypeID = (int)StandardUsageTypes.Public;

                    if (item["SubscriptionRequiredFlag"].ToString().ToUpper() == "TRUE")
                    {
                        //membership required
                        cp.UsageTypeID = (int)StandardUsageTypes.Public_MembershipRequired;
                    }
                    else
                    {
                        if (item["PaymentRequiredFlag"].ToString().ToUpper() == "TRUE")
                        {
                            //payment required
                            cp.UsageTypeID = (int)StandardUsageTypes.Public_PayAtLocation;
                        }
                        else
                        {
                            //accessible 24 hours, payment not required and membership not required, assume public
                            if (item["Accessible24Hours"].ToString().ToUpper() == "TRUE")
                            {
                                cp.UsageTypeID = (int)StandardUsageTypes.Public;
                            }
                        }
                    }

                    //special usage cases detected from text
                    if (cp.AddressInfo.ToString().ToLower().Contains("no public access"))
                    {
                        cp.UsageTypeID = (int)StandardUsageTypes.PrivateRestricted;
                    }

                    //add connections
                    var connectorList = item["Connector"].ToArray();
                    foreach (var conn in connectorList)
                    {
                        var cinfo = new ConnectionInfo() { };

                        if (conn["RatedOutputkW"] != null)
                        {
                            double tmpKw = 0;
                            if (double.TryParse(conn["RatedOutputkW"].ToString(), out tmpKw))
                            {
                                cinfo.PowerKW = tmpKw;
                            }
                        }

                        if (conn["RatedOutputVoltage"] != null)
                        {
                            var tmpV = 0;
                            if (int.TryParse(conn["RatedOutputVoltage"].ToString(), out tmpV))
                            {
                                cinfo.Voltage = tmpV;
                            }
                        }

                        if (conn["RatedOutputCurrent"] != null)
                        {
                            var tmpA = 0;
                            if (int.TryParse(conn["RatedOutputCurrent"].ToString(), out tmpA))
                            {
                                cinfo.Amps = tmpA;
                            }
                        }

                        var connectorType = conn["ConnectorType"].ToString();

                        if (!String.IsNullOrEmpty(connectorType))
                        {

                            cinfo.Reference = conn["ConnectorId"].ToString();

                            if (connectorType.ToUpper().Contains("BS 1363") || connectorType.ToUpper().Contains("3-PIN TYPE G (BS1363)"))
                            {
                                cinfo.ConnectionTypeID = (int)StandardConnectionTypes.BS1363TypeG; //UK 13 amp plug
                                cinfo.LevelID = 2; // default to level 2
                            }

                            if (connectorType.ToUpper() == "IEC 62196-2 TYPE 1 (SAE J1772)" || connectorType.ToUpper() == "TYPE 1 SAEJ1772 (IEC 62196)")
                            {
                                cinfo.ConnectionTypeID = (int)StandardConnectionTypes.J1772;
                                cinfo.LevelID = 2; // default to level 2
                            }

                            if (connectorType.ToUpper() == "IEC 62196-2 TYPE 2" || connectorType.ToUpper().Contains("(IEC62196)"))
                            {
                                cinfo.ConnectionTypeID = (int)StandardConnectionTypes.MennekesType2;
                                cinfo.LevelID = 2;

                                if (cinfo.Amps > 32)
                                {
                                    // assume connector is tethered due to high current
                                    cinfo.ConnectionTypeID = (int)StandardConnectionTypes.MennekesType2Tethered;
                                }

                                // handle Type 2 Tesla (IEC62196) DC which are Type 2 but tesla access only 
                                if (connectorType.ToUpper() == "TYPE 2 TESLA (IEC62196) DC")
                                {

                                    cinfo.Comments = "Tesla Only";
                                }
                            }

                            if (connectorType.ToUpper() == "JEVS G 105 (CHADEMO)" || connectorType.ToUpper() == "JEVS G105 (CHADEMO) DC")
                            {
                                cinfo.ConnectionTypeID = (int)StandardConnectionTypes.CHAdeMO;
                                cinfo.LevelID = 3;
                            }

                            if (connectorType.ToUpper() == "IEC 62196-2 TYPE 3")
                            {
                                cinfo.ConnectionTypeID = 26; //IEC 62196-2 type 3
                                cinfo.LevelID = 2;
                            }

                            if (connectorType.ToUpper() == "TYPE 2 COMBO (IEC62196) DC")
                            {
                                cinfo.ConnectionTypeID = (int)StandardConnectionTypes.CCSComboType2;
                                cinfo.LevelID = 3;
                            }

                            if (connectorType.ToUpper() == "TYPE 2 COMBO (IEC62196) DC")
                            {
                                cinfo.ConnectionTypeID = (int)StandardConnectionTypes.CCSComboType2;
                                cinfo.LevelID = 3;
                            }

                            if (conn["ChargePointStatus"] != null)
                            {
                                cinfo.StatusTypeID = (int)StandardStatusTypes.Operational;
                                if (conn["ChargePointStatus"].ToString() == "Out of service") cinfo.StatusTypeID = (int)StandardStatusTypes.NotOperational;
                            }

                            if (conn["ChargeMethod"] != null && !String.IsNullOrEmpty(conn["ChargeMethod"].ToString()))
                            {
                                var method = conn["ChargeMethod"].ToString();
                                //Single Phase AC, Three Phase AC, DC
                                if (method == "Single Phase AC") cinfo.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                                if (method == "Three Phase AC") cinfo.CurrentTypeID = (int)StandardCurrentTypes.ThreePhaseAC;
                                if (method == "DC") cinfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                            }

                            if (cinfo.ConnectionTypeID == null)
                            {
                                if (!String.IsNullOrEmpty(connectorType))
                                {
                                    Log("Unknown connector type:" + connectorType);
                                }
                            }

                            if (cp.Connections == null)
                            {
                                cp.Connections = new List<ConnectionInfo>();
                            }
                            if (!IsConnectionInfoBlank(cinfo))
                            {
                                //TODO: skip items with blank address info
                                cp.Connections.Add(cinfo);
                            }
                        }
                    }

                    //apply data attribution metadata
                    if (cp.MetadataValues == null) cp.MetadataValues = new List<MetadataValue>();
                    cp.MetadataValues.Add(new MetadataValue { MetadataFieldID = (int)StandardMetadataFields.Attribution, ItemValue = DataAttribution });

                    if (cp.DataQualityLevel == null) cp.DataQualityLevel = 3;

                    if (cp.SubmissionStatusTypeID == null) cp.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Imported_Published;

                    if (!skipPOI)
                    {
                        outputList.Add(new ChargePoint(cp));
                        itemCount++;
                    }
                }
            }

            if (unmappedOperators.Any())
            {
                Log("Unmapped operators: ");
                foreach (var uo in unmappedOperators.OrderBy(u => u.Key))
                {
                    Log($"{uo.Key} :: {uo.Value}");
                }
            }

            return outputList;
        }

        //http://naveedahmad.co.uk/2010/01/08/normalizing-postcode-in-c/
        public string NormalizePostcode(string postcode)
        {
            //TODO: more sensible detection of postcode format CV32 5?? vs E1 0BH etc
            var origPostcode = postcode + "";
            //removes end and start spaces
            postcode = postcode.Trim();
            //removes in middle spaces
            postcode = postcode.Replace(" ", "");

            switch (postcode.Length)
            {
                //add space after 2 characters if length is 5
                case 5: postcode = postcode.Insert(2, " "); break;
                //add space after 3 characters if length is 6
                case 6: postcode = postcode.Insert(3, " "); break;
                //add space after 4 characters if length is 7
                case 7: postcode = postcode.Insert(4, " "); break;

                default: break;
            }

            if (origPostcode != postcode)
            {
                System.Diagnostics.Debug.WriteLine("Postcode normalized: " + origPostcode + "->" + postcode);
            }
            return postcode;
        }
    }
}
