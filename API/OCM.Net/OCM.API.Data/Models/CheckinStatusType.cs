using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class CheckinStatusType
    {
        public CheckinStatusType()
        {
            this.UserComments = new List<UserComment>();
        }

        public byte ID { get; set; }
        public string Title { get; set; }
        public Nullable<bool> IsPositive { get; set; }
        public bool IsAutomatedCheckin { get; set; }
        public virtual ICollection<UserComment> UserComments { get; set; }
    }
}
