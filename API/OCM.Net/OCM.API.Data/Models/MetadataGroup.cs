using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class MetadataGroup
    {
        public MetadataGroup()
        {
            this.MetadataFields = new List<MetadataField>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public bool IsRestrictedEdit { get; set; }
        public int DataProviderID { get; set; }
        public bool IsPublicInterest { get; set; }
        public virtual DataProvider DataProvider { get; set; }
        public virtual ICollection<MetadataField> MetadataFields { get; set; }
    }
}
