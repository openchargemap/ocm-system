using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class DataType
    {
        public DataType()
        {
            MetadataFields = new HashSet<MetadataField>();
            SystemConfigs = new HashSet<SystemConfig>();
        }

        public byte Id { get; set; }
        public string Title { get; set; }

        public virtual ICollection<MetadataField> MetadataFields { get; set; }
        public virtual ICollection<SystemConfig> SystemConfigs { get; set; }
    }
}
