using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
