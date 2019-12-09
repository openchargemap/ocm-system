using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.API.Web.Models
{
    public class SystemInfoResult
    {
        public string SystemVersion { get; set; }
        public string DataVersionTimestamp { get; set; }
        public string DataVersionHash { get; set; }
    }
}
