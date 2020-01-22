using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class ConnectionInfo
    {
        public static Model.ConnectionInfo FromDataModel(Core.Data.ConnectionInfo s, bool isVerboseMode, Model.CoreReferenceData refData)
        {
            if (s == null) return null;

            var connectionInfo = new Model.ConnectionInfo()
            {
                ID = s.Id,
                Reference = s.Reference,
                Amps = s.Amps,
                Voltage = s.Voltage,
                PowerKW = s.PowerKw,
                Quantity = s.Quantity,
                Comments = s.Comments
            };

            //connection type (full object or id only)
            connectionInfo.ConnectionTypeID = s.ConnectionTypeId;

            if (isVerboseMode)
            {
                connectionInfo.ConnectionType = refData.ConnectionTypes.FirstOrDefault(c => c.ID == s.ConnectionTypeId);
            }


            //status type (full object or id only)
            connectionInfo.StatusTypeID = s.StatusTypeId;
            if (isVerboseMode)
            {
                connectionInfo.StatusType = refData.StatusTypes.FirstOrDefault(i => i.ID == s.StatusTypeId);
            }


            connectionInfo.CurrentTypeID = s.CurrentTypeId;
            if (isVerboseMode)
            {
                connectionInfo.CurrentType = refData.CurrentTypes.FirstOrDefault(i => i.ID == connectionInfo.CurrentTypeID);
            }

            // if PowerKW not manually supplied, attempt to compute it

            if (connectionInfo.PowerKW == null || connectionInfo.PowerKW == 0)
            {
                connectionInfo.PowerKW = ConnectionInfo.ComputePowerkW(connectionInfo);
            }

            // determine legacy charging 'level' (SAE definition) based on kw/voltage if available
            // if can't be computed use existing user supplied value (if any)

            connectionInfo.LevelID = ComputeChargingLevel(connectionInfo);

            if (connectionInfo.LevelID == null) connectionInfo.LevelID = s.LevelTypeId;

            if (isVerboseMode)
            {
                connectionInfo.Level = refData.ChargerTypes.FirstOrDefault(i => i.ID == connectionInfo.LevelID);
            }

            return connectionInfo;
        }

        public static int? ComputeChargingLevel(Model.ConnectionInfo c)
        {
            if (c.PowerKW > 0)
            {


                if (c.PowerKW < 2.4 || c.Voltage <= 120)
                {
                    // low power/voltage
                    // SAE Level 1, unit is probably AC output
                    return 1;
                }
                else if (c.CurrentTypeID cinfo.CurrentTypeID == (int)StandardCurrentTypes.SinglePhaseAC)
                {
                    // medium power/voltage AC
                    // SAE Level 2, unit is probably AC output
                    return 2;
                }
                else if (c.CurrentTypeID == (int)StandardCurrentTypes.ThreePhaseAC)
                {
                    if (c.PowerKW < 40 || c.Amps < 60)
                    {
                        return 2;
                    } else {
                        // Typically these are 43kW Type 2 charge connectors, used for rapid AC charging of some Renault vehicles
                        return 3;
                    }
                }
                else if (c.CurrentTypeID == (int)StandardCurrentTypes.DC && (c.PowerKW > 19.2 || c.Amps > 80 || c.Voltage > 400))
                {
                    // DC charging/high power
                    // SAE Level 3, unit is probably DC output
                    return 3;
                }
            }

            return null;
        }

        public static double? ComputePowerkW(Common.Model.ConnectionInfo cinfo)
        {
            var powerkW = cinfo.PowerKW;

            if (cinfo.Amps > 0 && cinfo.Voltage > 0)
            {
                if (cinfo.CurrentTypeID == null || cinfo.CurrentTypeID == (int)StandardCurrentTypes.SinglePhaseAC || cinfo.CurrentTypeID == (int)StandardCurrentTypes.DC)
                {
                    powerkW = ((double)cinfo.Amps * (double)cinfo.Voltage / 1000);
                }
                else
                {
                    powerkW = ((double)cinfo.Amps * (double)cinfo.Voltage * 1.732 / 1000);
                }

                powerkW = Math.Round((double)powerkW, 1);
            }

            return powerkW;
        }
    }


}
