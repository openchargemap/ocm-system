using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class MetadataField
    {
        public MetadataField()
        {
            MetadataFieldOptions = new HashSet<MetadataFieldOption>();
            MetadataValues = new HashSet<MetadataValue>();
        }

        public int Id { get; set; }
        public int MetadataGroupId { get; set; }
        public string Title { get; set; }
        public byte DataTypeId { get; set; }

        public virtual DataType DataType { get; set; }
        public virtual MetadataGroup MetadataGroup { get; set; }
        public virtual ICollection<MetadataFieldOption> MetadataFieldOptions { get; set; }
        public virtual ICollection<MetadataValue> MetadataValues { get; set; }
    }
}
