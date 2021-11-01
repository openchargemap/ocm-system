using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class MetadataGroup
    {
        public MetadataGroup()
        {
            MetadataFields = new HashSet<MetadataField>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsRestrictedEdit { get; set; }
        public int DataProviderId { get; set; }
        public bool IsPublicInterest { get; set; }

        public virtual DataProvider DataProvider { get; set; }
        public virtual ICollection<MetadataField> MetadataFields { get; set; }
    }
}
