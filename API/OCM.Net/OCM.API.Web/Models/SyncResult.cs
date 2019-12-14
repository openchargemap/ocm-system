using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OCM.API.Common.Model;

namespace OCM.API.Web.Models
{
    public class SyncResult
    {
        public SystemInfoResult SystemInfo { get; set; }
        public CoreReferenceData ReferenceData { get; set; }
        public IEnumerable<ChargePoint> UpdatedPOIs { get; set; }
    }
}
