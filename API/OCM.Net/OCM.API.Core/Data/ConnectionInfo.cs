using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class ConnectionInfo
    {
        public int Id { get; set; }
        public int ChargePointId { get; set; }
        public int ConnectionTypeId { get; set; }
        public string Reference { get; set; }
        public int? StatusTypeId { get; set; }
        public int? Amps { get; set; }
        public int? Voltage { get; set; }
        public double? PowerKw { get; set; }
        public int? LevelTypeId { get; set; }
        public int? Quantity { get; set; }
        public string Comments { get; set; }
        public short? CurrentTypeId { get; set; }

        public virtual ChargePoint ChargePoint { get; set; }
        public virtual ConnectionType ConnectionType { get; set; }
        public virtual CurrentType CurrentType { get; set; }
        public virtual ChargerType LevelType { get; set; }
        public virtual StatusType StatusType { get; set; }
    }
}
