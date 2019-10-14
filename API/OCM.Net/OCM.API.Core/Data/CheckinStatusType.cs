using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class CheckinStatusType
    {
        public CheckinStatusType()
        {
            UserComments = new HashSet<UserComment>();
        }

        public byte Id { get; set; }
        public string Title { get; set; }
        public bool? IsPositive { get; set; }
        public bool IsAutomatedCheckin { get; set; }

        public virtual ICollection<UserComment> UserComments { get; set; }
    }
}
