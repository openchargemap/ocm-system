using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class MetadataField
    {
        public MetadataField()
        {
            this.MetadataFieldOptions = new List<MetadataFieldOption>();
            this.MetadataValues = new List<MetadataValue>();
        }

        public int ID { get; set; }
        public int MetadataGroupID { get; set; }
        public string Title { get; set; }
        public byte DataTypeID { get; set; }
        public virtual DataType DataType { get; set; }
        public virtual MetadataGroup MetadataGroup { get; set; }
        public virtual ICollection<MetadataFieldOption> MetadataFieldOptions { get; set; }
        public virtual ICollection<MetadataValue> MetadataValues { get; set; }
    }
}
