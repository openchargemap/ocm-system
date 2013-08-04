using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model
{
    public class MetadataValue
    {
        public int ID { get; set; }
        //public int ChargePointID { get; set; }
        public int MetadataFieldID { get; set; }
        public string ItemValue { get; set; }
        public MetadataFieldOption MetadataFieldOption { get; set; }
    }
}
