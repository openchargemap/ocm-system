using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class MetadataValue
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }
        public int MetadataFieldID { get; set; }
        public string ItemValue { get; set; }
        public Nullable<int> MetadataFieldOptionID { get; set; }
        public virtual ChargePoint ChargePoint { get; set; }
        public virtual MetadataField MetadataField { get; set; }
        public virtual MetadataFieldOption MetadataFieldOption { get; set; }
    }
}
