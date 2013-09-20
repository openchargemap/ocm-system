using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class UsageType
    {
        public UsageType()
        {
            this.ChargePoints = new List<ChargePoint>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public Nullable<bool> IsPayAtLocation { get; set; }
        public Nullable<bool> IsMembershipRequired { get; set; }
        public Nullable<bool> IsAccessKeyRequired { get; set; }
        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
    }
}
