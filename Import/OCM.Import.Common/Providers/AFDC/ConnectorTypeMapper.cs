using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Import.Providers.AFDC;

public class ConnectorTypeMapper
{
    /// <summary>
    ///     Returns a Dictionary where the key is the ev_connector_type used by AFDC and the value is the ChargePoint.ConnectionTypeID.
    ///     https://developer.nrel.gov/docs/transportation/alt-fuel-stations-v1/all/
    /// </summary>
    public static Dictionary<string, StandardConnectionTypes> Create()
    {
        var dictionary = new Dictionary<string, StandardConnectionTypes>
        {
            // Level 1
            { "NEMA515", StandardConnectionTypes.Nema5_15 }, // NEMA 5-15 (J1772 AC)
            { "NEMA520", StandardConnectionTypes.Nema5_20 }, // NEMA 5-20 (J1772 AC)
            { "NEMA1450", StandardConnectionTypes.Nema14_50 }, // NEMA 14-50 (J1772 AC)
            // Level 2
            { "J1772", StandardConnectionTypes.J1772 }, // J1772 (AC)
            // Level 3
            { "CHADEMO", StandardConnectionTypes.CHAdeMO }, // CHAdeMO (DC)
            { "J1772COMBO", StandardConnectionTypes.CCSComboType1 }, // CCS (DC)
            { "TESLA", StandardConnectionTypes.TeslaSupercharger }, // NACS (J3400 AC/DC)
            { "J3271", StandardConnectionTypes.Unknown }, // MCS (J3271 DC)

        };
        return dictionary;
    }
}