using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class ConnectionType
    {
        public ConnectionType()
        {
            ConnectionInfoes = new HashSet<ConnectionInfo>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string FormalName { get; set; }
        public bool? IsDiscontinued { get; set; }
        public bool? IsObsolete { get; set; }

        public virtual ICollection<ConnectionInfo> ConnectionInfoes { get; set; }
    }
}
