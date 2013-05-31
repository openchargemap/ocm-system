using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class StatusType : SimpleReferenceDataType
    {
        public bool? IsOperational { get; set; }
    }
}