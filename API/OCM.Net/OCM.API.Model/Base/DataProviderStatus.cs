using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class DataProviderStatusType : SimpleReferenceDataType
    {
        public bool IsProviderEnabled { get; set; }
    }
}