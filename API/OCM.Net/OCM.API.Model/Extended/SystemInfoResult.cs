using System;

namespace OCM.API.Common.Model
{
    public class SystemInfoResult
    {
        public string SystemVersion { get; set; }
        public DateTime? POIDataLastModified { get; set; }
        public DateTime? POIDataLastCreated { get; set; }
        public int? MaxPOIId { get; set; }
        public string DataHash { get; set; }
    }
}
