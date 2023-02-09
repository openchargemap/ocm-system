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
            AutoRefreshURL = "http://chargepoints.dft.gov.uk/api/retrieve/registry/format/json";
            IsAutoRefreshed = true;
            IsProductionReady = true;
            MergeDuplicatePOIEquipment = false;
            DataAttribution = "Contains public sector information licensed under the Open Government Licence v2.0. http://www.nationalarchives.gov.uk/doc/open-government-licence/version/2/";
            DataProviderID = 18;//UK Chargepoint Registry
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
            List<ChargePoint> outputList = new List<ChargePoint>();

            string source = InputData;

            JObject o = JObject.Parse(source);

            var dataList = o["ChargeDevice"].ToArray();

            int itemCount = 0;

            foreach (var dataItem in dataList)
            {
                bool skipPOI = false;
                var item = dataItem;
                var cp = new POIDetails();

                var deviceName = item["ChargeDeviceName"].ToString();

                //private addresses are skipped from import
                if (!String.IsNullOrEmpty(deviceName) && deviceName.ToLower().Contains("parkatmyhouse"))
                {
                    skipPOI = true;
                }

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
                cp.AddressInfo.AddressLine1 = (String.IsNullOrEmpty(addressDetails["Street"].ToString()) ? addressDetails["BuildingNumber"].ToString() + " " + addressDetails["Thoroughfare"].ToString() : addressDetails["Street"].ToString().Replace("<br>", ", ")).Trim();
                cp.AddressInfo.Title = String.IsNullOrEmpty(locationDetails["LocationShortDescription"].ToString()) ? cp.AddressInfo.AddressLine1 : locationDetails["LocationShortDescription"].ToString();
                cp.AddressInfo.Title = cp.AddressInfo.Title.Replace("&amp;", "&");
                cp.AddressInfo.Title = cp.AddressInfo.Title.Replace("<br>", ", ").Trim();
                if (cp.AddressInfo.Title.Length > 100) cp.AddressInfo.Title = cp.AddressInfo.Title.Substring(0, 64) + "..";
                cp.AddressInfo.Town = addressDetails["PostTown"].ToString();
                string dependantLocality = addressDetails["DependantLocality"].ToString();
                if (!String.IsNullOrEmpty(dependantLocality) && dependantLocality.ToLower() != cp.AddressInfo.Town.ToLower())
                {
                    //use depenendantLocality if provided and is not same as town
                    cp.AddressInfo.AddressLine2 = dependantLocality;
                }
                cp.AddressInfo.Postcode = addressDetails["PostCode"].ToString();
                cp.AddressInfo.Latitude = double.Parse(locationDetails["Latitude"].ToString());
                cp.AddressInfo.Longitude = double.Parse(locationDetails["Longitude"].ToString());
                cp.AddressInfo.AccessComments = locationDetails["LocationLongDescription"].ToString().Replace("<br>", ", ").Replace("\r\n", ", ").Replace("\n", ", ");

                //if title is empty, attempt to add a suitable replacement
                if (String.IsNullOrEmpty(cp.AddressInfo.Title))
                {
                    if (!String.IsNullOrEmpty(cp.AddressInfo.AddressLine1))
                    {
                        cp.AddressInfo.Title = cp.AddressInfo.AddressLine1.Trim();
                    }
                    else
                    {
                        cp.AddressInfo.Title = cp.AddressInfo.Postcode;
                    }
                }

                if (cp.AddressInfo.Title == "NA" && cp.AddressInfo.AddressLine1 == "NA")
                {
                    // item needs an address resolved
                    cp.AddressInfo.Title = "";
                    cp.AddressCleaningRequired = true;
                    cp.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Imported_UnderReview;

                }

                if (cp.AddressInfo.Title.ToLower().StartsWith("asset no.") && cp.AddressInfo.AddressLine1.ToLower().StartsWith("asset no."))
                {
                    // item needs an address resolved
                    cp.AddressInfo.Title = "";
                    cp.AddressCleaningRequired = true;
                    cp.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Imported_UnderReview;
                }

                //cp.AddressInfo.ContactTelephone1 = item["phone"].ToString();

                if (!String.IsNullOrEmpty(addressDetails["Country"].ToString()))
                {
                    string country = addressDetails["Country"].ToString();
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
                else
                {
                    //default to US if no country identified
                    //cp.AddressInfo.Country = cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(cy => cy.ID == 2);
                }

                //operator from DeviceController
                var deviceController = item["DeviceController"];

                cp.AddressInfo.RelatedURL = deviceController["Website"].ToString();
                var deviceOperator = coreRefData.Operators.FirstOrDefault(devOp => devOp.Title.ToLower().Trim().Contains(deviceController["OrganisationName"].ToString().ToLower().Trim()));
                if (deviceOperator != null)
                {
                    cp.OperatorID = deviceOperator.ID;
                }
                else
                {
                    //operator from device owner
                    var devOwner = item["DeviceOwner"];
                    deviceOperator = coreRefData.Operators.FirstOrDefault(devOp => devOp.Title.ToLower().Trim().Contains(devOwner["OrganisationName"].ToString().ToLower().Trim()));
                    if (deviceOperator != null)
                    {
                        cp.OperatorID = deviceOperator.ID;
                    }
                }

                if (cp.OperatorID == null)
                {
                    string operatorName = deviceController["OrganisationName"]?.ToString() ?? item["DeviceOwner"]["OrganisationName"]?.ToString();

                    // match specific operators

                    switch (operatorName)
                    {
                        case "Chargemaster (POLAR)":
                            cp.OperatorID = 8;
                            break;
                        case "Ecotricity (Electric Highway)":
                            cp.OperatorID = 24;
                            break;
                        case "ecar NI":
                            cp.OperatorID = 91;
                            break;
                        case "eo Charging":
                            cp.OperatorID = 3298;
                            break;
                        case "Alfa Power":
                            cp.OperatorID = 3326;
                            break;
                        case "InCharge - an initiative by Vattenfall":
                            cp.OperatorID = 3343;
                            break;
                        case "APT":
                            cp.OperatorID = 3341;
                            break;
                        case "ecars ESB":
                            cp.OperatorID = 22;
                            break;
                        case "GRIDSERVE Sustainable Energy":
                            cp.OperatorID = 3430;
                            break;
                        case "ESB EV Solutions":
                            cp.OperatorID = 3357;
                            break;
                        case "ChargePoint Network (Netherlands) B.V.":
                            cp.OperatorID = 5;
                            break;
                        case "SureCharge/FM Conway":
                            cp.OperatorID = 3612;
                            break;
                        case "EV-Dot":
                            cp.OperatorID = 3446;
                            break;
                        case "Clenergy EV":
                            cp.OperatorID = 3605;
                            break;
                        case "ScottishPower":
                            cp.OperatorID = 3537;
                            break;
                        case "IONITY GmbH":
                            cp.OperatorID = 3299;
                            break;
                        case "BP-Pulse (POLAR)":
                            cp.OperatorID = 32;
                            break;
                    }

                    if (cp.OperatorID == null)
                    {
                        Log("Unknown Operator: " + deviceController["OrganisationName"]?.ToString() ?? item["DeviceOwner"]["OrganisationName"]?.ToString());
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
                    ConnectionInfo cinfo = new ConnectionInfo() { };

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
                        int tmpV = 0;
                        if (int.TryParse(conn["RatedOutputVoltage"].ToString(), out tmpV))
                        {
                            cinfo.Voltage = tmpV;
                        }
                    }

                    if (conn["RatedOutputCurrent"] != null)
                    {
                        int tmpA = 0;
                        if (int.TryParse(conn["RatedOutputCurrent"].ToString(), out tmpA))
                        {
                            cinfo.Amps = tmpA;
                        }
                    }

                    string connectorType = conn["ConnectorType"].ToString();

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
                            string method = conn["ChargeMethod"].ToString();
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

            return outputList;
        }

        //http://naveedahmad.co.uk/2010/01/08/normalizing-postcode-in-c/
        public string NormalizePostcode(string postcode)
        {
            //TODO: more sensible detection of postcode format CV32 5?? vs E1 0BH etc
            string origPostcode = postcode + "";
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
