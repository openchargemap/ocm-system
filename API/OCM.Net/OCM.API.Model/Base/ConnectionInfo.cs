using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class ConnectionInfo
    {
        public int ID { get; set; }
        public ConnectionType ConnectionType { get; set; }
        public string Reference { get; set; }
        public StatusType StatusType { get; set; }
        public ChargerType Level { get; set; }
        public int? Amps { get; set; }
        public int? Voltage { get; set; }
        public double? PowerKW { get; set; }
        public CurrentType CurrentType { get; set; }
        public int? Quantity { get; set; }
        public string Comments { get; set; }
    }
}