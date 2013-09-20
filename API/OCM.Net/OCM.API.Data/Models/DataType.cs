using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class DataType
    {
        public DataType()
        {
            this.MetadataFields = new List<MetadataField>();
            this.SystemConfigs = new List<SystemConfig>();
        }

        public byte ID { get; set; }
        public string Title { get; set; }
        public virtual ICollection<MetadataField> MetadataFields { get; set; }
        public virtual ICollection<SystemConfig> SystemConfigs { get; set; }
    }
}
