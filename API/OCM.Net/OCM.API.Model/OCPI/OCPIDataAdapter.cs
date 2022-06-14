using OCM.Model.OCPI;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OCM.API.Common.Model.OCPI
{
    public class OCPIDataAdapter
    {
        CoreReferenceData _coreReferenceData { get; set; }

        private List<RegionInfo> _countries = new List<RegionInfo>();
        public OCPIDataAdapter(CoreReferenceData coreReferenceData)
        {
            _coreReferenceData = coreReferenceData;

            // create lookup list for countries
            _countries = new List<RegionInfo>();
            foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                RegionInfo country = new RegionInfo(culture.LCID);
                if (_countries.Where(p => p.Name == country.Name).Count() == 0)
                    _countries.Add(country);
            }
        }

        private string GetCountryCodeFromISO3(string srcISO)
        {

            return _countries.FirstOrDefault(c => c.ThreeLetterISORegionName == srcISO).TwoLetterISORegionName;

        }
        public IEnumerable<OCM.API.Common.Model.ChargePoint> FromOCPI(IEnumerable<OCM.Model.OCPI.Location> source)
        {
            var output = new List<ChargePoint>();

            foreach (var i in source)
            {

                var iso2Code = i.Country_code;

                if (string.IsNullOrEmpty(iso2Code) && i.Country != null)
                {
                    GetCountryCodeFromISO3(i.Country);
                }

                var cp = new ChargePoint
                {
                    DataProvidersReference = i.Id,
                    AddressInfo = new AddressInfo
                    {
                        Title = i.Name,
                        AddressLine1 = i.Address,
                        Town = i.City,
                        Latitude = double.Parse(i.Coordinates.Latitude),
                        Longitude = double.Parse(i.Coordinates.Longitude),
                        CountryID = _coreReferenceData.Countries.FirstOrDefault(c => c.ISOCode == iso2Code)?.ID,
                        AccessComments = i.Directions?.Select(d => d.Text).ToString()
                    },
                    Connections = new List<ConnectionInfo>()
                };

                if (i.Evses?.Any() == true)
                {
                    // TODO map status

                    foreach (var e in i.Evses)
                    {
                        foreach (var c in e.Connectors)
                        {
                            var evse_id = e.Evse_id ?? e.Uid;

                            var connectionInfo = new ConnectionInfo
                            {
                                Reference = c.Id,
                                PowerKW = c.Max_electric_power == 0 ? null : c.Max_electric_power,
                                Voltage = c.Max_voltage,
                                Amps = c.Max_amperage
                            };

                            // set power type
                            if (c.Power_type == ConnectorPower_type.DC)
                            {
                                connectionInfo.CurrentTypeID = (int)StandardCurrentTypes.DC;
                            }
                            else if (c.Power_type == ConnectorPower_type.AC_1_PHASE)
                            {
                                connectionInfo.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                            }
                            else if (c.Power_type == ConnectorPower_type.AC_3_PHASE)
                            {
                                connectionInfo.CurrentTypeID = (int)StandardCurrentTypes.ThreePhaseAC;
                            }

                            // calc power kw if not specified
                            if (connectionInfo.PowerKW == 0 || connectionInfo.PowerKW == null)
                            {
                                connectionInfo.PowerKW = ConnectionInfo.ComputePowerkW(connectionInfo);
                            }

                            // set status
                            // set connector type
                            connectionInfo.ConnectionTypeID = ConnectionTypeFromStandard(c.Standard, c.Format);


                            cp.Connections.Add(connectionInfo);
                        }
                    }
                    output.Add(cp);
                }

            }
            return output;
        }

        public int? ConnectionTypeFromStandard(ConnectorStandard standard, ConnectorFormat format)
        {
            var mapping = new Dictionary<ConnectorStandard, int>
            {
                { ConnectorStandard.UNKNOWN,(int)StandardConnectionTypes.Unknown }, // unknown is not an official part of the OCPI spec
                { ConnectorStandard.CHADEMO, (int)StandardConnectionTypes.CHAdeMO },
                { ConnectorStandard.DOMESTIC_A, (int)StandardConnectionTypes.Unknown }, // NEMA 1-15, 2 pins
                { ConnectorStandard.DOMESTIC_B, (int)StandardConnectionTypes.Nema5_15 },
                { ConnectorStandard.DOMESTIC_C, (int)StandardConnectionTypes.Europlug },
                { ConnectorStandard.DOMESTIC_D, (int)StandardConnectionTypes.Unknown }, // type "D", 3 pin
                { ConnectorStandard.DOMESTIC_E, (int)StandardConnectionTypes.Unknown }, // type "E", CEE 7/5 3 pins
                { ConnectorStandard.DOMESTIC_F, (int)StandardConnectionTypes.Schuko },
                { ConnectorStandard.DOMESTIC_G, (int)StandardConnectionTypes.BS1363TypeG },
                { ConnectorStandard.DOMESTIC_H, (int)StandardConnectionTypes.Unknown }, // type "H", SI-32, 3 pins
                { ConnectorStandard.DOMESTIC_I, (int)StandardConnectionTypes.AS3112 }, // type "I", AS 3112, 3 pins
                { ConnectorStandard.DOMESTIC_J, (int)StandardConnectionTypes.Unknown }, // type "J", SEV 1011, 3 pins
                { ConnectorStandard.DOMESTIC_K, (int)StandardConnectionTypes.Unknown }, // type "K", DS 60884-2-D1, 3 pins
                { ConnectorStandard.DOMESTIC_L, (int)StandardConnectionTypes.Unknown }, // type "L", CEI 23-16-VII, 3 pins
                { ConnectorStandard.IEC_60309_2_single_16, 34}, // IEC 60309-2 Industrial Connector single phase 16 amperes (usually blue)
                { ConnectorStandard.IEC_60309_2_three_16,35 }, // IEC 60309-2 Industrial Connector three phase 16 amperes (usually red)
                { ConnectorStandard.IEC_60309_2_three_32,35 }, // IEC 60309-2 Industrial Connector three phase 32 amperes (usually red)
                { ConnectorStandard.IEC_60309_2_three_64,35 }, // IEC 60309-2 Industrial Connector three phase 64 amperes (usually red)
                { ConnectorStandard.IEC_62196_T1,(int)StandardConnectionTypes.J1772 }, //IEC 62196 Type 1 "SAE J1772"
                { ConnectorStandard.IEC_62196_T1_COMBO,(int)StandardConnectionTypes.CCSComboType1 },
                { ConnectorStandard.IEC_62196_T2,(int)StandardConnectionTypes.MennekesType2 },
                { ConnectorStandard.IEC_62196_T2_COMBO,(int)StandardConnectionTypes.CCSComboType2 },
                { ConnectorStandard.IEC_62196_T3A,(int)StandardConnectionTypes.Unknown },// IEC 62196 Type 3A
                { ConnectorStandard.IEC_62196_T3C,(int)StandardConnectionTypes.Type3 },// IEC 62196 Type 3C / SCAME
                { ConnectorStandard.PANTOGRAPH_BOTTOM_UP,(int)StandardConnectionTypes.Unknown },//On-board Bottom-up-Pantograph typically for bus charging
                { ConnectorStandard.PANTOGRAPH_TOP_DOWN,(int)StandardConnectionTypes.Unknown },//Off-board Top-down-Pantograph typically for bus charging
                { ConnectorStandard.TESLA_R,(int)StandardConnectionTypes.TeslaRoadster },
                { ConnectorStandard.TESLA_S,(int)StandardConnectionTypes.TeslaProprietary },
            };

            var mappedConnectorId = mapping[standard];

            // distinguish between mennekes socket vs tethered
            if (mappedConnectorId == (int)StandardConnectionTypes.MennekesType2 && format == ConnectorFormat.SOCKET)
            {
                mappedConnectorId = (int)StandardConnectionTypes.MennekesType2Tethered;
            }

            return mappedConnectorId;

        }
    }
}