using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.AFDC;

public class ConnectionsMapper
{
    private readonly StandardConnectionTypes[] _acConnectionTypes =
    [
        StandardConnectionTypes.Nema5_15,
        StandardConnectionTypes.Nema5_20,
        StandardConnectionTypes.Nema14_50,
        StandardConnectionTypes.J1772
    ];

    private readonly Dictionary<string, StandardConnectionTypes> _connectorTypeMapping = ConnectorTypeMapper.Create();

    public List<ConnectionInfo> Map(JToken[] evChargingUnits,
        ChargerType chargerLevel1, ChargerType chargerLevel2, ChargerType chargerLevel3)
    {
        var connections = new Dictionary<string, ConnectionInfo>();
        foreach (var unit in evChargingUnits)
        foreach (var connectorType in _connectorTypeMapping.Keys)
        {
            var unitConnector = unit["connectors"]?[connectorType];
            var portCount = (unitConnector?["port_count"] ?? 0).Value<int>();
            if (portCount <= 0) continue;

            var chargerLevel = GetChargerLevel(connectorType, chargerLevel1, chargerLevel2, chargerLevel3);
            var connectionTypeId = (int)_connectorTypeMapping[connectorType];
            var powerKW = (unitConnector?["power_kw"] ?? 0).Value<double>();
            var connectionsKey = connectionTypeId + "|" + powerKW;

            if (connections.TryGetValue(connectionsKey, out var connection))
            {
                connection.Quantity += portCount;
                continue;
            }

            connections.Add(connectionsKey,
                new ConnectionInfo
                {
                    Quantity = portCount,
                    ConnectionTypeID = connectionTypeId,
                    CurrentTypeID = (int)GetCurrentType(connectorType),
                    PowerKW = powerKW,
                    Level = chargerLevel,
                    LevelID = chargerLevel?.ID
                }
            );
        }

        return [.. connections.Values];
    }

    private static ChargerType GetChargerLevel(string connectorType, ChargerType chargerLevel1,
        ChargerType chargerLevel2, ChargerType chargerLevel3)
    {
        return connectorType switch
        {
            "NEMA515" or "NEMA520" or "NEMA1450" => chargerLevel1,
            "J1772" => chargerLevel2,
            "CHADEMO" or "J1772COMBO" or "TESLA" or "J3271" => chargerLevel3,
            _ => null
        };
    }

    private StandardCurrentTypes GetCurrentType(string connectorType)
    {
        return _acConnectionTypes.Contains(_connectorTypeMapping[connectorType])
            ? StandardCurrentTypes.SinglePhaseAC
            : StandardCurrentTypes.DC;
    }
}