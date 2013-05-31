using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class Country : SimpleReferenceDataType
    {
        public string ISOCode { get; set; }
    }
}