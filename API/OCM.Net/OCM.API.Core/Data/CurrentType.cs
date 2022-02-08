using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class CurrentType
    {
        public CurrentType()
        {
            ConnectionInfos = new HashSet<ConnectionInfo>();
        }

        public short Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public virtual ICollection<ConnectionInfo> ConnectionInfos { get; set; }
    }
}
