using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model
{
    public class MetadataField : SimpleReferenceDataType
    {
        public int MetadataGroupID { get; set; }
        public int DataTypeID { get; set; }
        public MetadataGroup MetadataGroup { get; set; }
        public DataType DataType { get; set; }
    }
}
