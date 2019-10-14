using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class SubmissionStatusType
    {
        public SubmissionStatusType()
        {
            ChargePoints = new HashSet<ChargePoint>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsLive { get; set; }

        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
    }
}
