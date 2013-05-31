using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class ConnectionType : SimpleReferenceDataType
    {
        public string FormalName { get; set; }
        public bool? IsDiscontinued { get; set; }
        public bool? IsObsolete { get; set; }
    }
}