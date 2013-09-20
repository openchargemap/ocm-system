using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class CurrentType
    {
        public CurrentType()
        {
            this.ConnectionInfoes = new List<ConnectionInfo>();
        }

        public short ID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public virtual ICollection<ConnectionInfo> ConnectionInfoes { get; set; }
    }
}
