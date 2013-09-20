using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class MetadataFieldOption
    {
        public MetadataFieldOption()
        {
            this.MetadataValues = new List<MetadataValue>();
        }

        public int ID { get; set; }
        public int MetadataFieldID { get; set; }
        public string Title { get; set; }
        public virtual MetadataField MetadataField { get; set; }
        public virtual ICollection<MetadataValue> MetadataValues { get; set; }
    }
}
