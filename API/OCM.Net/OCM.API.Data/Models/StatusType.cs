using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class StatusType
    {
        public StatusType()
        {
            this.ChargePoints = new List<ChargePoint>();
            this.ConnectionInfoes = new List<ConnectionInfo>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public Nullable<bool> IsOperational { get; set; }
        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
        public virtual ICollection<ConnectionInfo> ConnectionInfoes { get; set; }
    }
}
