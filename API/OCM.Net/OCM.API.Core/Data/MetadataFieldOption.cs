using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class MetadataFieldOption
    {
        public MetadataFieldOption()
        {
            MetadataValues = new HashSet<MetadataValue>();
        }

        public int Id { get; set; }
        public int MetadataFieldId { get; set; }
        public string Title { get; set; }

        public virtual MetadataField MetadataField { get; set; }
        public virtual ICollection<MetadataValue> MetadataValues { get; set; }
    }
}
