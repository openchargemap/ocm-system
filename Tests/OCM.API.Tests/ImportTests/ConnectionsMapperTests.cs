using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using OCM.Import.Providers.AFDC;
using Xunit;

namespace OCM.API.Tests.ImportTests;

public class ConnectionsMapperTests
{
    [Fact]
    public void Process_MapsOperationalPublicCanadianStationWithLevel2Connections()
    {
        // Given
        var chargingUnitLevel1 = CreateDefaultEvChargingUnits();
        chargingUnitLevel1["connectors"]?["NEMA1450"]?["power_kw"] = 12.0;
        chargingUnitLevel1["connectors"]?["NEMA1450"]?["port_count"] = 3;
        
        var chargingUnitLevel2 = CreateDefaultEvChargingUnits();
        chargingUnitLevel2["connectors"]?["J1772"]?["power_kw"] = 6.2;
        chargingUnitLevel2["connectors"]?["J1772"]?["port_count"] = 2;
        
        var chargingUnitLevel3A = CreateDefaultEvChargingUnits();
        chargingUnitLevel3A["connectors"]?["CHADEMO"]?["power_kw"] = 50.0;
        chargingUnitLevel3A["connectors"]?["CHADEMO"]?["port_count"] = 1;
        chargingUnitLevel3A["connectors"]?["J1772COMBO"]?["power_kw"] = 50.0;
        chargingUnitLevel3A["connectors"]?["J1772COMBO"]?["port_count"] = 1;

        var chargingUnitLevel3B = CreateDefaultEvChargingUnits();
        chargingUnitLevel3B["connectors"]?["J1772COMBO"]?["power_kw"] = 50.0;
        chargingUnitLevel3B["connectors"]?["J1772COMBO"]?["port_count"] = 4;

        var chargingUnitLevel3C = CreateDefaultEvChargingUnits();
        chargingUnitLevel3C["connectors"]?["TESLA"]?["power_kw"] = 180.0;
        chargingUnitLevel3C["connectors"]?["TESLA"]?["port_count"] = 1;
        chargingUnitLevel3C["connectors"]?["J1772COMBO"]?["power_kw"] = 180.0;
        chargingUnitLevel3C["connectors"]?["J1772COMBO"]?["port_count"] = 1;


        JToken[] evChargingUnits =
        [
            chargingUnitLevel1,
            chargingUnitLevel2,
            chargingUnitLevel3A,
            chargingUnitLevel3B,
            chargingUnitLevel3C
        ];

        var chargerLevel1 = new ChargerType
        {
            ID = 1
        };
        var chargerLevel2 = new ChargerType
        {
            ID = 2
        };
        var chargerLevel3 = new ChargerType
        {
            ID = 3
        };

        // When
        var results =
            new ConnectionsMapper().Map(evChargingUnits, chargerLevel1, chargerLevel2, chargerLevel3);

        // Then
        Assert.Equal(6, results.Count);

        var result = results[0];
        Assert.Equal(3, result.Quantity);
        Assert.Equal((int)StandardConnectionTypes.Nema14_50, result.ConnectionTypeID);
        Assert.Equal((int)StandardCurrentTypes.SinglePhaseAC, result.CurrentTypeID);
        Assert.Equal(12.0, result.PowerKW);
        Assert.Equal(chargerLevel1, result.Level);
        Assert.Equal(chargerLevel1.ID, result.LevelID);
        
        result = results[1];
        Assert.Equal(2, result.Quantity);
        Assert.Equal((int)StandardConnectionTypes.J1772, result.ConnectionTypeID);
        Assert.Equal((int)StandardCurrentTypes.SinglePhaseAC, result.CurrentTypeID);
        Assert.Equal(6.2, result.PowerKW);
        Assert.Equal(chargerLevel2, result.Level);
        Assert.Equal(chargerLevel2.ID, result.LevelID);

        result = results[2];
        Assert.Equal(1, result.Quantity);
        Assert.Equal((int)StandardConnectionTypes.CHAdeMO, result.ConnectionTypeID);
        Assert.Equal((int)StandardCurrentTypes.DC, result.CurrentTypeID);
        Assert.Equal(50.0, result.PowerKW);
        Assert.Equal(chargerLevel3, result.Level);
        Assert.Equal(chargerLevel3.ID, result.LevelID);

        result = results[3];
        Assert.Equal(5, result.Quantity);
        Assert.Equal((int)StandardConnectionTypes.CCSComboType1, result.ConnectionTypeID);
        Assert.Equal((int)StandardCurrentTypes.DC, result.CurrentTypeID);
        Assert.Equal(50.0, result.PowerKW);
        Assert.Equal(chargerLevel3, result.Level);
        Assert.Equal(chargerLevel3.ID, result.LevelID);

        result = results[4];
        Assert.Equal(1, result.Quantity);
        Assert.Equal((int)StandardConnectionTypes.CCSComboType1, result.ConnectionTypeID);
        Assert.Equal((int)StandardCurrentTypes.DC, result.CurrentTypeID);
        Assert.Equal(180.0, result.PowerKW);
        Assert.Equal(chargerLevel3, result.Level);
        Assert.Equal(chargerLevel3.ID, result.LevelID);

        result = results[5];
        Assert.Equal(1, result.Quantity);
        Assert.Equal((int)StandardConnectionTypes.TeslaSupercharger, result.ConnectionTypeID);
        Assert.Equal((int)StandardCurrentTypes.DC, result.CurrentTypeID);
        Assert.Equal(180.0, result.PowerKW);
        Assert.Equal(chargerLevel3, result.Level);
        Assert.Equal(chargerLevel3.ID, result.LevelID);
    }

    private static JToken CreateDefaultEvChargingUnits()
    {
        var json = """
                   {
                     "network": "FLO",
                     "connectors": {
                       "J1772": {
                         "power_kw": null,
                         "port_count": 0
                       },
                       "J3271": {
                         "power_kw": null,
                         "port_count": 0
                       },
                       "TESLA": {
                         "power_kw": null,
                         "port_count": 0
                       },
                       "CHADEMO": {
                         "power_kw": null,
                         "port_count": 0
                       },
                       "NEMA515": {
                         "power_kw": null,
                         "port_count": 0
                       },
                       "NEMA520": {
                         "power_kw": null,
                         "port_count": 0
                       },
                       "NEMA1450": {
                         "power_kw": null,
                         "port_count": 0
                       },
                       "J1772COMBO": {
                         "power_kw": null,
                         "port_count": 0
                       }
                     },
                     "port_count": 1,
                     "charging_level": "dc_fast",
                     "funding_sources": []
                   }
                   """;
        return JObject.Parse(json);
    }
}