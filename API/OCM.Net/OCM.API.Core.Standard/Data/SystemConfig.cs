using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class SystemConfig
    {
        public string ConfigKeyName { get; set; }
        public string ConfigValue { get; set; }
        public byte? DataTypeId { get; set; }

        public virtual DataType DataType { get; set; }
    }
}
