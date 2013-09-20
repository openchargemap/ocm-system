using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class ConnectionType
    {
        public ConnectionType()
        {
            this.ConnectionInfoes = new List<ConnectionInfo>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public string FormalName { get; set; }
        public Nullable<bool> IsDiscontinued { get; set; }
        public Nullable<bool> IsObsolete { get; set; }
        public virtual ICollection<ConnectionInfo> ConnectionInfoes { get; set; }
    }
}
