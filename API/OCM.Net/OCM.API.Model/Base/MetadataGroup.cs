using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model
{
    public class MetadataGroup : SimpleReferenceDataType
    {
        public int GroupOwnerUserID { get; set; }
        public bool IsRestrictedEdit { get; set; }

        public User GroupOwner { get; set; }
    }
}
