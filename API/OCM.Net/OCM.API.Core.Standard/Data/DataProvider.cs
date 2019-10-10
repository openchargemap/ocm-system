using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class DataProvider
    {
        public DataProvider()
        {
            ChargePoints = new HashSet<ChargePoint>();
            MetadataGroups = new HashSet<MetadataGroup>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string WebsiteUrl { get; set; }
        public string Comments { get; set; }
        public int? DataProviderStatusTypeId { get; set; }
        public bool IsRestrictedEdit { get; set; }
        public bool? IsOpenDataLicensed { get; set; }
        public bool? IsApprovedImport { get; set; }
        public string License { get; set; }
        public DateTime? DateLastImported { get; set; }

        public virtual DataProviderStatusType DataProviderStatusType { get; set; }
        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
        public virtual ICollection<MetadataGroup> MetadataGroups { get; set; }
    }
}
