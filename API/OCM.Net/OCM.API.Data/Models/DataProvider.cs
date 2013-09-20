using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class DataProvider
    {
        public DataProvider()
        {
            this.ChargePoints = new List<ChargePoint>();
            this.DataProviderUsers = new List<DataProviderUser>();
            this.MetadataGroups = new List<MetadataGroup>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public string WebsiteURL { get; set; }
        public string Comments { get; set; }
        public Nullable<int> DataProviderStatusTypeID { get; set; }
        public bool IsRestrictedEdit { get; set; }
        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
        public virtual DataProviderStatusType DataProviderStatusType { get; set; }
        public virtual ICollection<DataProviderUser> DataProviderUsers { get; set; }
        public virtual ICollection<MetadataGroup> MetadataGroups { get; set; }
    }
}
