using OCM.Model.OCPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OCM.API.Common.Model.OCPI
{
    public class OCPIDataAdapter
    {

        CoreReferenceData _coreReferenceData { get; set; }
        bool _useLiveStatus { get; set; }

        private readonly List<RegionInfo> _countries = new();

        internal Dictionary<string, int> _unmappedOperators = new Dictionary<string, int>();

        public OCPIDataAdapter(CoreReferenceData coreReferenceData, bool useLiveStatus = false)
        {
            _coreReferenceData = coreReferenceData;

            // create lookup list for countries
            _countries = new List<RegionInfo>();
            foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                RegionInfo country = new(culture.Name);
                if (!_countries.Where(p => p.Name == country.Name).Any())
                    _countries.Add(country);
            }
            _useLiveStatus = useLiveStatus;
        }

        private string GetCountryCodeFromISO3(string srcISO)
        {
            return _countries.FirstOrDefault(c => c.ThreeLetterISORegionName == srcISO).TwoLetterISORegionName;
        }

        /// <summary>
        /// Map from a set of OCPI locations to a set of OCM ChargePoint
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public IEnumerable<OCM.API.Common.Model.ChargePoint> FromOCPI(IEnumerable<OCM.Model.OCPI.Location> source, int dataProviderId, Dictionary<string, int> operatorMappings = null)
        {

            foreach (var i in source)
            {

                var iso2Code = i.Country_code;

                if (string.IsNullOrEmpty(iso2Code) && i.Country != null)
                {
                    iso2Code = GetCountryCodeFromISO3(i.Country);
                }

                var cp = new ChargePoint
                {
                    DataProvidersReference = i.Id,
                    DataProviderID = dataProviderId,
                    AddressInfo = new AddressInfo
                    {
                        Title = i.Name ?? i.Address,
                        AddressLine1 = i.Address,
                        Town = i.City,
                        Postcode = i.Postal_code,
                        Latitude = double.Parse(i.Coordinates.Latitude),
                        Longitude = double.Parse(i.Coordinates.Longitude),
                        CountryID = _coreReferenceData.Countries.FirstOrDefault(c => c.ISOCode == iso2Code)?.ID,
                        AccessComments = i.Directions?.Select(d => d.Text).ToString()
                    },
                    Connections = new List<ConnectionInfo>()
                };

                List<OCM.Model.OCPI.Evse> evse = new();

                if (i.Evses?.Any() == true)
                {
                    evse = new List<Evse>(i.Evses);
                }
                else if (i.AdditionalProperties.ContainsKey("evses"))
                {
                    // Older OCPI has EVSE list as an additional property
                    evse = (List<OCM.Model.OCPI.Evse>)(i.AdditionalProperties["evses"]);
                }

                // TODO: map status at per EVSE group level

                foreach (var e in evse)
                {
                    cp.StatusTypeID = MapOCMStatusTypeFromStatus(e.Status, _useLiveStatus);
                    foreach (var c in e.Connectors)
                    {
                        var evse_id = e.Evse_id ?? e.Uid;

                        var connectionInfo = new ConnectionInfo
                        {
                            Reference = c.Id,
                            PowerKW = c.Max_electric_power == 0 ? null : c.Max_electric_power / 1000,
                            Voltage = c.Max_voltage > 0 ? c.Max_voltage : null,
                            Amps = c.Max_amperage > 0 ? c.Max_amperage : null,
                            // set power type
                            CurrentTypeID = MapOCMPowerTypeFromOCPI(c.Power_type),
                            StatusTypeID = cp.StatusTypeID,
                            Quantity = 1
                        };

                    // calc power kw if not specified
                    if (connectionInfo.PowerKW == 0 || connectionInfo.PowerKW == null)
                    {
                        connectionInfo.PowerKW = ConnectionInfo.ComputePowerkW(connectionInfo);
                    }

                    // set status
                    // set connector type
                    connectionInfo.ConnectionTypeID = MapOCMConnectionTypeFromStandard(c.Standard, c.Format);

                    cp.Connections.Add(connectionInfo);
                }
            }

            // map operator
            if (i.Operator != null && !string.IsNullOrEmpty(i.Operator.Name))
            {
                if (operatorMappings != null && operatorMappings.ContainsKey(i.Operator.Name))
                {
                    cp.OperatorID = operatorMappings[i.Operator.Name];
                }
                else
                {
                    if (!_unmappedOperators.ContainsKey(i.Operator.Name))
                    {
                        _unmappedOperators.Add(i.Operator.Name, 1);
                    }
                    else
                    {
                        _unmappedOperators[i.Operator.Name]++;
                    }
                }
            }
            else
            {
                // operator may be stored as party id
                if (!string.IsNullOrEmpty(i.Party_id))
                {
                    if (operatorMappings != null && operatorMappings.ContainsKey(i.Party_id))
                    {
                        cp.OperatorID = operatorMappings[i.Party_id];
                    }
                    else
                    {
                        if (!_unmappedOperators.ContainsKey(i.Party_id))
                        {
                            _unmappedOperators.Add(i.Party_id, 1);
                        }
                        else
                        {
                            _unmappedOperators[i.Party_id]++;
                        }
                    }
                }
            }

            yield return cp;
        }
    }

    /// <summary>
    /// Get the list of operators we failed to match
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, int> GetUnmappedOperators()
    {
        return _unmappedOperators;
    }

    /// <summary>
    /// Map from a set of OCM ChargePoints locations to a set of OCPI Locations
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static IEnumerable<OCM.Model.OCPI.Location> ToOCPI(IEnumerable<OCM.API.Common.Model.ChargePoint> source)
    {
        foreach (var i in source)
        {

            var poi = new Location
            {
                Id = i.ID.ToString(),
                City = i.AddressInfo.Town,
                Address = i.AddressInfo.AddressLine1,
                Coordinates = new GeoLocation { Latitude = i.AddressInfo.Latitude.ToString(), Longitude = i.AddressInfo.Latitude.ToString() },
                Country_code = i.AddressInfo.Country.ISOCode,
                Last_updated = i.DateLastVerified != null ? ToRfc3339String(i.DateLastVerified.Value) : null,
                Postal_code = i.AddressInfo.Postcode,
                State = i.AddressInfo.StateOrProvince,
                Name = i.AddressInfo.Title,
                Directions = !string.IsNullOrEmpty(i.AddressInfo.AccessComments) ? new DisplayText[] { new DisplayText { Text = i.AddressInfo.AccessComments } } : null,
                Evses = new List<Evse>()
            };

            // TODO: map status at per EVSE group level
            var evse = new Evse { Connectors = new List<Connector>() };

            foreach (var e in i.Connections)
            {
                var conn = new Connector
                {
                    Id = e.ID.ToString(),
                    Format = ConnectorFormat.CABLE,
                    Max_voltage = (int)e.Voltage,
                    Max_amperage = (int)e.Amps,
                    Max_electric_power = (int)e.PowerKW,
                    Standard = MapOCPIConnectionFromOCM(e.ConnectionTypeID),
                    Power_type = MapOCPIPowerTypeFromOCM(e.CurrentTypeID)
                };

                evse.Status = MapOCPIStatusFromOCM(e.StatusTypeID ?? i.StatusTypeID);

                evse.Connectors.Add(conn);

            }

            yield return poi;
        }
    }

    private static ConnectorPower_type MapOCPIPowerTypeFromOCM(int? currentTypeID)
    {
        var mapping = GetPowerTypeMapping();
        return mapping.FirstOrDefault(m => m.Value == currentTypeID).Key;
    }

    private static int MapOCMPowerTypeFromOCPI(ConnectorPower_type type)
    {
        var mapping = GetPowerTypeMapping();
        return mapping[type];
    }

    private static ConnectorStandard MapOCPIConnectionFromOCM(int? connectionTypeId)
    {
        var mapping = GetConnectionTypeMapping();
        var connectionType = mapping.FirstOrDefault(m => m.Value == connectionTypeId);
        return connectionType.Key;
    }

    private static EvseStatus MapOCPIStatusFromOCM(int? statusTypeId)
    {
        var mapping = GetStatusMapping();
        var status = mapping.FirstOrDefault(m => m.Value == statusTypeId);
        return status.Key;
    }

    public static int? MapOCMStatusTypeFromStatus(EvseStatus status, bool useLiveStatus = true)
    {
        var mapping = GetStatusMapping();

        var mappedStatusId = mapping[status];

        if (!useLiveStatus)
        {
            if (status == EvseStatus.AVAILABLE || status == EvseStatus.CHARGING || status == EvseStatus.RESERVED || status == EvseStatus.BLOCKED)
            {
                mappedStatusId = (int)StandardStatusTypes.Operational;
            }
        }

        return mappedStatusId;
    }

    public static int? MapOCMConnectionTypeFromStandard(ConnectorStandard standard, ConnectorFormat format)
    {
        var mapping = GetConnectionTypeMapping();

        var mappedConnectorId = mapping[standard];

        // distinguish between mennekes socket vs tethered
        if (mappedConnectorId == (int)StandardConnectionTypes.MennekesType2 && format == ConnectorFormat.SOCKET)
        {
            mappedConnectorId = (int)StandardConnectionTypes.MennekesType2Tethered;
        }

        return mappedConnectorId;

    }

    public static string ToRfc3339String(DateTime dateTime)
    {
        // based on https://sebnilsson.com/blog/c-datetime-to-rfc3339-iso-8601/
        return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo);
    }

    private static Dictionary<ConnectorPower_type, int> GetPowerTypeMapping()
    {
        return new Dictionary<ConnectorPower_type, int>
            {
                { ConnectorPower_type.DC,(int)StandardCurrentTypes.DC },
                { ConnectorPower_type.AC_1_PHASE,(int)StandardCurrentTypes.SinglePhaseAC },
                { ConnectorPower_type.AC_3_PHASE,(int)StandardCurrentTypes.ThreePhaseAC }
            };
    }

    private static Dictionary<ConnectorStandard, int> GetConnectionTypeMapping()
    {
        return new Dictionary<ConnectorStandard, int>
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
                { ConnectorStandard.GBT_AC, (int)StandardConnectionTypes.GB_T_AC }, //GB-T AC - GB/T 20234.2 (Socket)
                { ConnectorStandard.GBT_DC, (int)StandardConnectionTypes.GB_T_DC }, // GB-T DC - GB/T 20234.3
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
    }

    private static Dictionary<EvseStatus, int> GetStatusMapping()
    {
        return new Dictionary<EvseStatus, int>
            {
                { EvseStatus.UNKNOWN,(int)StandardStatusTypes.Unknown },
                { EvseStatus.AVAILABLE, (int)StandardStatusTypes.CurrentlyAvailable },
                { EvseStatus.BLOCKED, (int)StandardStatusTypes.TemporarilyUnavailable },
                { EvseStatus.CHARGING, (int)StandardStatusTypes.CurrentlyInUse },
                { EvseStatus.INOPERATIVE, (int)StandardStatusTypes.NotOperational },
                { EvseStatus.OUTOFORDER, (int)StandardStatusTypes.NotOperational },
                { EvseStatus.PLANNED, (int)StandardStatusTypes.PlannedForFutureDate },
                { EvseStatus.REMOVED, (int)StandardStatusTypes.RemovedDecomissioned },
                { EvseStatus.RESERVED, (int)StandardStatusTypes.CurrentlyInUse }
            };
    }
}
}