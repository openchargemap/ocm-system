using System.Collections.Generic;

namespace OCM.API.Common.Model
{
    public class MetadataField : SimpleReferenceDataType
    {
        public int MetadataGroupID { get; set; }
        public int DataTypeID { get; set; }

        public DataType DataType { get; set; }
        public List<MetadataFieldOption> MetadataFieldOptions { get; set; }
    }
}
