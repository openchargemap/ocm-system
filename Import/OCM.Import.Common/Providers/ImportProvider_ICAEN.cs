using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.Import.Providers
{
    public class ImportProvider_ICAEN : BaseImportProvider, IImportProvider
    {
        public ImportProvider_ICAEN()
        {
            ProviderName = "ICAEN";
            OutputNamePrefix = "icaen";
            AutoRefreshURL = "http://icaen.gencat.cat/web/sites/icaen/.content/12_opendata/arxius/CATALUNYA_PdR_VEHICLES_ELECTRICS.csv";
            IsAutoRefreshed = true;
            IsProductionReady = true;
            SourceEncoding = Encoding.GetEncoding("Windows-1252");
            DataProviderID = 25;//ICAEN
        }

        public List<ChargePoint> Process(CoreReferenceData coreRefData)
        {
            this.ImportRefData = new CommonImportRefData(coreRefData);

            //whole file cleanup (broken newline in text)
            InputData = InputData.Replace("\n\";", "\";");

            var results = InputData.Split('\n');
            var keyLookup = results[0].Replace("\r", "").Split(';').ToList();
            List<ChargePoint> outputList = new List<ChargePoint>();
            int rowIndex = 0;
            foreach (var row in results)
            {
                //skip first row or empty rows
                if (rowIndex > 0 && !String.IsNullOrEmpty(row))
                {
                    if (!(String.IsNullOrEmpty(row.Replace(";", "").Trim())))
                    {
                        var cols = row.Replace("\r", "").Replace(";", " ;").Split(';');
                        var poi = new ChargePoint();

                        var title = cols[keyLookup.FindIndex(a => a == "DESIGNACIÓ-DESCRIPTIVA")];
                        var usageType = cols[keyLookup.FindIndex(a => a == "ACCES")];
                        var operatorName = cols[keyLookup.FindIndex(a => a == "PROMOTOR-GESTOR")];
                        var speed = cols[keyLookup.FindIndex(a => a == "TIPUS VELOCITAT")];
                        var connectionType = cols[keyLookup.FindIndex(a => a == "TIPUS CONNEXIÓ")];
                        var latitude = cols[keyLookup.FindIndex(a => a == "LATITUD")];
                        var longitude = cols[keyLookup.FindIndex(a => a == "LONGITUD")];

                        var reference = cols[keyLookup.FindIndex(a => a == "IDE PDR")];
                        var address = cols[keyLookup.FindIndex(a => a == "ADREÇA")];
                        var province = cols[keyLookup.FindIndex(a => a == "PROVINCIA")];
                        var city = cols[keyLookup.FindIndex(a => a == "MUNICIPI")];
                        var numStations = cols[keyLookup.FindIndex(a => a == "NPLACES ESTACIÓ")];
                        var vehicleTypes = cols[keyLookup.FindIndex(a => a == "TIPUS VEHICLE")];
                        var telephone = cols[keyLookup.FindIndex(a => a == "TELEFON")];
                        var hours = cols[keyLookup.FindIndex(a => a == "HORARI")];
                        var additionalInfo = cols[keyLookup.FindIndex(a => a == "INFORMACIÓ ADICIONAL")];

                        poi.DataProviderID = DataProviderID;
                        poi.DataProvidersReference = reference;
                        poi.AddressInfo = new AddressInfo();

                        poi.AddressInfo.Title = title;
                        poi.AddressInfo.AddressLine1 = address.Trim();
                        poi.AddressInfo.Town = city;
                        if (city != province) poi.AddressInfo.StateOrProvince = province;
                        poi.AddressInfo.Latitude = double.Parse(latitude);
                        poi.AddressInfo.Longitude = double.Parse(longitude);
                        poi.AddressInfo.ContactTelephone1 = telephone;
                        poi.AddressInfo.AccessComments = hours;
                        poi.GeneralComments = additionalInfo;

                        //TODO: Operator and Operators Reference

                        if (usageType.StartsWith("VIA PUBLICA")) usageType = "VIA PUBLICA";
                        //resolve usage type
                        switch (usageType.Trim())
                        {
                            case "APARCAMENT PUBLIC":
                            case "VIA PUBLICA":
                            case "VIA PUBLICA -VORERA":
                                poi.UsageType = ImportRefData.UsageType_Public;
                                break;

                            case "APARCAMENT CC":
                                poi.UsageType = ImportRefData.UsageType_PublicPayAtLocation;
                                break;

                            default:
                                Log("Unknown Usage Type: " + usageType);
                                break;
                        }

                        //parse equipment info
                        var stations = numStations.Split('+');
                        if (String.IsNullOrWhiteSpace(numStations)) stations = new string[] { "1" }; //blank, default to 1
                        var connectionTypes = connectionType.Split('+');

                        foreach (var c in connectionTypes)
                        {
                            if (poi.Connections == null) poi.Connections = new List<ConnectionInfo>();

                            var connection = new ConnectionInfo();

                            //connection.Quantity = int.Parse(ns.Trim());

                            switch (c.ToLower().Trim())
                            {
                                case "chademo":
                                    connection.ConnectionTypeID = (int)StandardConnectionTypes.CHAdeMO;
                                    break;

                                case "mennekes":
                                case "mennekes.m":
                                    connection.ConnectionTypeID = (int)StandardConnectionTypes.MennekesType2;
                                    break;

                                case "schuko":
                                case "mennekes.f":
                                    connection.ConnectionTypeID = (int)StandardConnectionTypes.Schuko;
                                    break;

                                case "ccs combo2":
                                    connection.ConnectionTypeID = (int)StandardConnectionTypes.CCSComboType2;
                                    break;

                                case "j1772":
                                    connection.ConnectionTypeID = (int)StandardConnectionTypes.J1772;
                                    break;

                                default:
                                    Log("Unknown Connection Type: " + c);
                                    break;
                            }

                            poi.StatusTypeID = (int)StandardStatusTypes.Operational;
                            //level/status
                            switch (speed.Trim())
                            {
                                case "NORMAL":
                                case "semiRAPID i NORMAL": //low quality description, default to lowest spec
                                case "semiRAPID":
                                    connection.LevelID = 2;
                                    connection.Voltage = 220;
                                    connection.Amps = 32;
                                    connection.PowerKW = (connection.Voltage * connection.Amps) / 1000;
                                    connection.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                                    connection.StatusTypeID = (int)StandardStatusTypes.Operational;
                                    break;

                                case "RAPID":
                                case "superRAPID":
                                    connection.LevelID = 3;
                                    connection.Voltage = 400;
                                    connection.Amps = 100;
                                    connection.PowerKW = (connection.Voltage * connection.Amps) / 1000;
                                    connection.CurrentTypeID = (int)StandardCurrentTypes.DC;
                                    connection.StatusTypeID = (int)StandardStatusTypes.Operational;
                                    break;

                                case "FORA DE SERVEI":
                                    connection.StatusTypeID = (int)StandardStatusTypes.NotOperational;
                                    poi.StatusTypeID = (int)StandardStatusTypes.NotOperational;
                                    break;

                                case "DESMANTELLADA":
                                    connection.StatusTypeID = (int)StandardStatusTypes.RemovedDecomissioned;
                                    poi.StatusTypeID = (int)StandardStatusTypes.RemovedDecomissioned;
                                    break;

                                default:
                                    Log("Unknown Speed Type: " + speed);
                                    break;
                            }
                            poi.Connections.Add(connection);
                        }

                        //vehicle type metadata:
                        poi.MetadataValues = new List<MetadataValue>();

                        if (vehicleTypes.Contains("cotxe"))
                        {
                            poi.MetadataValues.Add(new MetadataValue { MetadataFieldID = (int)StandardMetadataFields.VehicleType, MetadataFieldOptionID = (int)StandardMetadataFieldOptions.Car });
                        }

                        if (vehicleTypes.Contains("moto"))
                        {
                            poi.MetadataValues.Add(new MetadataValue { MetadataFieldID = (int)StandardMetadataFields.VehicleType, MetadataFieldOptionID = (int)StandardMetadataFieldOptions.Motorbike });
                        }

                        if (vehicleTypes.Contains("mercaderies"))
                        {
                            poi.MetadataValues.Add(new MetadataValue { MetadataFieldID = (int)StandardMetadataFields.VehicleType, MetadataFieldOptionID = (int)StandardMetadataFieldOptions.DeliveryVehicle });
                        }

                        //TODO: get status of first equipment item and use that as status for overall poi

                        if (poi.DataQualityLevel == null) poi.DataQualityLevel = 2;

                        poi.SubmissionStatus = ImportRefData.SubmissionStatus_Imported;

                        outputList.Add(poi);
                    }
                }
                rowIndex++;
            }
            return outputList;
        }
    }
}