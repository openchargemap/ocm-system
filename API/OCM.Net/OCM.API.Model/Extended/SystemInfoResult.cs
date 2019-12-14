using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.API.Common.Model
{
    public class SystemInfoResult
    {
        public string SystemVersion { get; set; }
        public DateTime POIDataLastModified { get; set; }
        public string DataHash { get; set; }
    }
}
