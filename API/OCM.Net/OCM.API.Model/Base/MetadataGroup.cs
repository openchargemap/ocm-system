using System.Collections.Generic;

namespace OCM.API.Common.Model
{
    public class MetadataGroup : SimpleReferenceDataType
    {
        public int DataProviderID { get; set; }

        public bool IsRestrictedEdit { get; set; }
        public bool IsPublicInterest { get; set; }

        public List<MetadataField> MetadataFields { get; set; }
    }
}
