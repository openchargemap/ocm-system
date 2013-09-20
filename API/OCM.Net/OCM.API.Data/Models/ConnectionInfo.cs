using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class ConnectionInfo
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }
        public int ConnectionTypeID { get; set; }
        public string Reference { get; set; }
        public Nullable<int> StatusTypeID { get; set; }
        public Nullable<int> Amps { get; set; }
        public Nullable<int> Voltage { get; set; }
        public Nullable<double> PowerKW { get; set; }
        public Nullable<int> LevelTypeID { get; set; }
        public Nullable<int> Quantity { get; set; }
        public string Comments { get; set; }
        public Nullable<short> CurrentTypeID { get; set; }
        public virtual ChargePoint ChargePoint { get; set; }
        public virtual ChargerType ChargerType { get; set; }
        public virtual CurrentType CurrentType { get; set; }
        public virtual ConnectionType ConnectionType { get; set; }
        public virtual StatusType StatusType { get; set; }
    }
}
