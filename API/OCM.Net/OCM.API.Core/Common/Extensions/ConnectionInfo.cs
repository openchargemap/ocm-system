using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class ConnectionInfo
    {
        public static Model.ConnectionInfo FromDataModel(Core.Data.ConnectionInfo s)
        {
            if (s == null) return null;

            var connectionInfo = new Model.ConnectionInfo() {
                ID = s.ID,
                Reference = s.Reference, 
                Amps = s.Amps, 
                Voltage = s.Voltage, 
                PowerKW = s.PowerKW,
                CurrentType = CurrentType.FromDataModel(s.CurrentType),
                Quantity = s.Quantity,
                Comments = s.Comments
            };
          
            if (s.ConnectionType != null)
            {
                connectionInfo.ConnectionType = ConnectionType.FromDataModel(s.ConnectionType);
            }

            if (s.StatusType != null)
            {
                connectionInfo.StatusType = StatusType.FromDataModel(s.StatusType);
            }

            if (s.ChargerType != null)
            {
                connectionInfo.Level = ChargerType.FromDataModel(s.ChargerType);
            }

            return connectionInfo;
        }
    }
}