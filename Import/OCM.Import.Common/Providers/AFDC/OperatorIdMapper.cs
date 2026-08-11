using System.Collections.Generic;

namespace OCM.Import.Providers.AFDC;

public static class OperatorIdMapper
{
    /// <summary>
    ///     Returns a Dictionary where the key is the ev_network used by AFDC and the value is the ChargePoint.OperatorID.
    ///     https://developer.nrel.gov/docs/transportation/alt-fuel-stations-v1/all/
    /// </summary>
    public static Dictionary<string, int> Create()
    {
        var dictionary = new Dictionary<string, int>
        {
            { "7CHARGE", 1 }, // 7Charge
            { "ABM", 1 }, // ABM
            { "AMPED_UP", 1 }, // AmpedUp! Networks
            { "AMPUP", 3619 }, // AmpUp
            { "APPLEGREEN", 3516 }, // applegreen electric
            { "AUTEL", 3968 }, // Autel
            { "BCHYDRO", 3385 }, // BC Hydro
            { "Blink Network", 9 }, // Blink
            { "BP_PULSE", 3788 }, // bp pulse
            { "CHAEVI", 1 }, // Chaevi
            { "CHARGELAB", 3621 }, // ChargeLab
            { "CHARGENET", 1 }, // ChargeNet
            { "ChargePoint Network", 5 }, // ChargePoint
            { "CHARGESMART_EV", 3864 }, // ChargeSmart EV
            { "CHARGEUP", 3734 }, // ChargeUP
            { "CHARGIE", 1 }, // Chargie
            { "CIRCLE_K", 3510 }, // CircleK Charge
            { "COUCHE_TARD", 3510 }, // CircleK/Couche-Tard Recharge
            { "Circuit électrique", 90 }, // Circuit électrique
            { "CORAL_EV", 1 }, // Coral EV
            { "DIRT_ROAD", 1 }, // DirtRoad
            { "eCharge Network", 3365 }, // eCharge Network
            { "ELECTRIC_ERA", 3789 }, // Electric Era
            { "Electrify America", 3318 }, // Electrify America
            { "Electrify Canada", 3400 }, // Electrify Canada
            { "ENERTECH", 1 }, // Enertech
            { "ENVIROSPARK", 1 }, // EnviroSpark
            { "EPIC_CHARGING", 1 }, // Epic Charging
            { "EVBOLT", 1 }, // EVBOLT
            { "EVCHRON", 1 }, // EVchron
            { "EV Connect", 3372 }, // EV Connect
            { "EVCS", 3542 }, // EVCS
            { "EVGATEWAY", 3389 }, // EvGateway
            { "eVgo Network", 15 }, // EVgo
            { "EVIUM", 1 }, // EVIUM Charging
            { "EVMATCH", 3725 }, // EVmatch
            { "EVOKE", 1 }, // Evoke Systems
            { "EVPASSPORT", 4003 }, // EVPassport
            { "EVPOWER", 1 }, // eVPower
            { "EVRANGE", 3526 }, // EV Range
            { "EVSTART", 3745 }, // EV Start
            { "EVXY", 1 }, // EVXY
            { "EZVOLTZ", 1 }, // ezVOLTz
            { "FILGO", 1 }, // Filgo
            { "FLASH", 3723 }, // FLASH
            { "FLIPTURN", 1 }, // Flipturn
            { "FLITWAY", 1 }, // Flitway
            { "FLO", 89 }, // FLO
            { "FORD_CHARGE", 1 }, // Ford Charge
            { "FPLEV", 3749 }, // FPL EVolution
            { "FCN", 3733 }, // Francis Energy
            { "GO_TO_U", 3494 }, // GO TO-U
            { "GRAVITI_ENERGY", 3695 }, // Graviti Energy
            { "GRAVITY_CHARGING_CENTER", 3695 }, // Gravity Charging Center
            { "GREEN_BRIDGE", 1 }, // Green Bridge EV Charging
            { "HONEY_BADGER", 1 }, // HoneyBadger Charging
            { "Hwisel", 3866 }, // Hwisel
            { "HYPERFUEL", 3762 }, // Hyperfuel
            { "IN_CHARGE", 3867 }, // InCharge
            { "INTERTIE", 1 }, // Intertie
            { "IONNA", 3831 }, // IONNA
            { "ITSELECTRIC", 1 }, // itselectric
            { "IVY", 3416 }, // Ivy
            { "JULE", 3488 }, // Jule
            { "KWEV", 1 }, // KWEV
            { "KWIK_CHARGE", 3825 }, // Kwik Charge by Kwik Trip
            { "LAKELAND_EV", 3761 }, // Lakeland EV CHARGING
            { "LOOP", 3757 }, // Loop
            { "MARCHE_EXPRESS", 1 }, // Marché Express Recharge Rapide
            { "MATCHA", 1 }, // Matcha Electric
            { "MERCEDES_BENZ", 3827 }, // Mercedes-Benz High-Power Charging
            { "NAYAX_ENERGY", 1 }, // Nayax Energy
            { "Non-Networked", 1 }, // Non-Networked
            { "NOODOE", 3346 }, // Noodoe
            { "NOVA_SCOTIA_POWER", 1 }, // Nova Scotia Power
            { "OBE_POWER", 1 }, // OBE Power
            { "OMNI_POTENTIAL", 1 }, // OmniPotential Energy Partners
            { "ONPOINT_EV_SOLUTIONS", 1 }, // OnPoint EV Solutions
            { "ON_THE_RUN_EV", 3756 }, // On the Run EV
            { "OpConnect", 107 }, // OpConnect
            { "PLUGEV", 1 }, // PlugEV
            { "POWERCHARGE", 1 }, // PowerCharge
            { "POWERFLEX", 3618 }, // PowerFlex
            { "POWERPORT_EVC", 1 }, // PowerPort EVC
            { "POWERPUMP", 1 }, // PowerPump
            { "POWERUP", 1 }, // PowerUp
            { "QUICKCHARGE", 1 }, // QuickCharge
            { "RACETRAC", 1 }, // RaceTrac
            { "RED_E", 3696 }, // Red E Charge
            { "REVEL", 3948 }, // Revel
            { "REVITALIZE", 217 }, // Revitalize Charging Solutions
            { "RIVIAN_ADVENTURE", 3607 }, // Rivian Adventure Network
            { "RIVIAN_WAYPOINTS", 3617 }, // Rivian Waypoints
            { "ROVE", 3824 }, // Rove
            { "SHELL_RECHARGE", 59 }, // Shell Recharge
            { "STAY_N_CHARGE", 1 }, // Stay-N-Charge
            { "Sun Country Highway", 51 }, // Sun Country Highway
            { "SURECHARGE", 1 }, // SureCharge
            { "SWTCH", 3493 }, // SWTCH Energy
            { "SYNERGEV", 1 }, // synergEV
            { "Tesla Destination", 23 }, // Tesla Destination
            { "Tesla", 23 }, // Tesla Supercharger
            { "TURNONGREEN", 3865 }, // TurnOnGreen
            { "UNIVERSAL", 3694 }, // Universal EV Chargers
            { "US_SUPERCHARGE", 1 }, // US Supercharge
            { "VIALYNK", 3863 }, // ViaLynk
            { "WALMART", 3969 }, // Walmart
            { "WATT_EV", 1 }, // WattEV
            { "WAVE", 3724 }, // WAVE
            { "WEVO", 1 }, // Wevo Energy
            { "XEAL", 1 }, // Xeal EV Charging
            { "ZEFNET", 3454 } // ZEF Network
        };
        return dictionary;
    }
}