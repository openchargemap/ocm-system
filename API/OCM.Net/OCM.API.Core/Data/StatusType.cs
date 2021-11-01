using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class StatusType
    {
        public StatusType()
        {
            ChargePoints = new HashSet<ChargePoint>();
            ConnectionInfoes = new HashSet<ConnectionInfo>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public bool? IsOperational { get; set; }
        public bool? IsUserSelectable { get; set; }

        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
        public virtual ICollection<ConnectionInfo> ConnectionInfoes { get; set; }
    }
}
