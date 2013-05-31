using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class ChargerType : SimpleReferenceDataType
    {
        public string Comments { get; set; }
        public bool IsFastChargeCapable { get; set; }
    }
}