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

            //connection type (full object or id only)
            if (isVerboseMode && (s.ConnectionTypeID!=null || s.ConnectionType != null))
            {
                connectionInfo.ConnectionType = ConnectionType.FromDataModel(s.ConnectionType);
                connectionInfo.ConnectionTypeID = s.ConnectionType.ID;
            }
            else
            {
                connectionInfo.ConnectionTypeID = s.ConnectionTypeID;
            }

            //status type (full object or id only)
            if (isVerboseMode && (s.StatusTypeID!=null || s.StatusType != null))
            {
                connectionInfo.StatusType = StatusType.FromDataModel(s.StatusType);
                connectionInfo.StatusTypeID = s.StatusTypeID;
            }
            else
            {
                if (s.StatusTypeID != null) connectionInfo.StatusTypeID = s.StatusTypeID;
            }

            //charging level type (full object or id only)
            if (isVerboseMode && (s.LevelTypeID!=null || s.ChargerType != null))
            {
                connectionInfo.Level = ChargerType.FromDataModel(s.ChargerType);
                connectionInfo.LevelID = s.ChargerType.ID;
            }
            else
            {
                if (s.LevelTypeID != null) connectionInfo.LevelID = s.LevelTypeID;
            }

            if (isVerboseMode && (s.CurrentTypeID!=null || s.CurrentType != null))
            {
                connectionInfo.CurrentType = CurrentType.FromDataModel(s.CurrentType);
                connectionInfo.CurrentTypeID = s.CurrentType.ID;
            }
            else
            {
                if (s.CurrentTypeID != null) connectionInfo.CurrentTypeID = s.CurrentTypeID;
            }

            return connectionInfo;
        }
    }
}