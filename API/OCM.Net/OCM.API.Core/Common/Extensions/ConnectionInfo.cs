using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class ConnectionInfo
    {
        public static Model.ConnectionInfo FromDataModel(Core.Data.ConnectionInfo s, bool isVerboseMode)
        {
            if (s == null) return null;

            var connectionInfo = new Model.ConnectionInfo()
            {
                ID = s.ID,
                Reference = s.Reference,
                Amps = s.Amps,
                Voltage = s.Voltage,
                PowerKW = s.PowerKW,
                Quantity = s.Quantity,
                Comments = s.Comments
            };

            if (s.ConnectionTypeID != null)
            {
                connectionInfo.ConnectionTypeID = s.ConnectionTypeID;
                if (isVerboseMode)
                {
                    connectionInfo.ConnectionType = ConnectionType.FromDataModel(s.ConnectionType);
                }
            }

            if (s.StatusTypeID != null)
            {
                connectionInfo.StatusTypeID = s.StatusTypeID;
                if (isVerboseMode)
                {
                    connectionInfo.StatusType = StatusType.FromDataModel(s.StatusType);
                }
            }

            if (s.LevelTypeID != null)
            {
                connectionInfo.LevelID = s.LevelTypeID;
                if (isVerboseMode)
                {
                    connectionInfo.Level = ChargerType.FromDataModel(s.ChargerType);
                }
            }

            if (s.CurrentTypeID != null)
            {
                connectionInfo.CurrentTypeID = s.CurrentTypeID;
                if (isVerboseMode)
                {
                    connectionInfo.CurrentType = CurrentType.FromDataModel(s.CurrentType);
                }
            }
            return connectionInfo;
        }
    }
}