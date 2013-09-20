using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class ChargerType
    {
        public ChargerType()
        {
            this.ConnectionInfoes = new List<ConnectionInfo>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public string Comments { get; set; }
        public bool IsFastChargeCapable { get; set; }
        public Nullable<int> DisplayOrder { get; set; }
        public virtual ICollection<ConnectionInfo> ConnectionInfoes { get; set; }
    }
}
