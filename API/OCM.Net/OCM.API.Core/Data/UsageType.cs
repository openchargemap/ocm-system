using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class UsageType
    {
        public UsageType()
        {
            ChargePoints = new HashSet<ChargePoint>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public bool? IsPayAtLocation { get; set; }
        public bool? IsMembershipRequired { get; set; }
        public bool? IsAccessKeyRequired { get; set; }
        public bool? IsPublicAccess { get; set; }

        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
    }
}
