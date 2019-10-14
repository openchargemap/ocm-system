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
                ID = s.Id,
                Reference = s.Reference,
                Amps = s.Amps,
                Voltage = s.Voltage,
                PowerKW = s.PowerKw,
                Quantity = s.Quantity,
                Comments = s.Comments
            };

            //connection type (full object or id only)
            if (isVerboseMode)
            {
                connectionInfo.ConnectionType = ConnectionType.FromDataModel(s.ConnectionType);
                connectionInfo.ConnectionTypeID = (s.ConnectionType != null ? s.ConnectionType.Id : s.ConnectionTypeId);
            }
            else
            {
                connectionInfo.ConnectionTypeID = s.ConnectionTypeId;
            }

            //status type (full object or id only)
            if (isVerboseMode && (s.StatusTypeId != null || s.StatusType != null))
            {
                connectionInfo.StatusType = StatusType.FromDataModel(s.StatusType);
                connectionInfo.StatusTypeID = s.StatusTypeId;
            }
            else
            {
                if (s.StatusTypeId != null) connectionInfo.StatusTypeID = s.StatusTypeId;
            }

            //charging level type (full object or id only)
            if (isVerboseMode && (s.LevelTypeId != null || s.LevelType != null))
            {
                connectionInfo.Level = ChargerType.FromDataModel(s.LevelType);
                connectionInfo.LevelID = s.LevelTypeId;
            }
            else
            {
                if (s.LevelTypeId != null) connectionInfo.LevelID = s.LevelTypeId;
            }

            if (isVerboseMode && (s.CurrentTypeId != null || s.CurrentType != null))
            {
                connectionInfo.CurrentType = CurrentType.FromDataModel(s.CurrentType);
                connectionInfo.CurrentTypeID = s.CurrentTypeId;
            }
            else
            {
                if (s.CurrentTypeId != null) connectionInfo.CurrentTypeID = s.CurrentTypeId;
            }

            return connectionInfo;
        }
    }
}