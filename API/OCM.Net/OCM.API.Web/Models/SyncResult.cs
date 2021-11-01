using OCM.API.Common.Model;
using System.Collections.Generic;

namespace OCM.API.Web.Models
{
    public class SyncResult
    {
        public SystemInfoResult SystemInfo { get; set; }
        public CoreReferenceData ReferenceData { get; set; }
        public IEnumerable<ChargePoint> UpdatedPOIs { get; set; }
    }
}
