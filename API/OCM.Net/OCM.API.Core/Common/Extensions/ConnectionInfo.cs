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

            // determine legacy charging 'level' (SAE definition) based on kw/voltage if available
            // if can't be computed use existing user supplied value (if any)

            connectionInfo.LevelID = ComputeChargingLevel(connectionInfo);

            if (connectionInfo.LevelID == null) connectionInfo.LevelID = s.LevelTypeId;

            if (isVerboseMode)
            {
                connectionInfo.Level = refData.ChargerTypes.FirstOrDefault(i => i.ID == connectionInfo.LevelID);
            }

            connectionInfo.CurrentTypeID = s.CurrentTypeId;
            if (isVerboseMode)
            {
                connectionInfo.CurrentType = refData.CurrentTypes.FirstOrDefault(i => i.ID == connectionInfo.CurrentTypeID);
            }

            return connectionInfo;
        }

        public static int? ComputeChargingLevel(Model.ConnectionInfo c)
        {
            if (c.PowerKW > 0)
            {

                if (c.PowerKW < 2.4 || c.Voltage <= 120)
                {
                    // SAE Level 1, unit is probably AC output
                    return 1;
                }
                else if (c.PowerKW >= 2 || c.Voltage > 200 && c.Voltage <= 400)
                {
                    // SAE Level 2, unit is probably AC output
                    return 2;
                }
                else if (c.PowerKW > 19.4 || c.Voltage > 400)
                {
                    // SAE Level 3, unit is probably DC output
                    return 3;
                }
            }

            return null;
        }
    }


}