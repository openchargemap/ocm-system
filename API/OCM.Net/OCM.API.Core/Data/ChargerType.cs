using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class ChargerType
    {
        public ChargerType()
        {
            ConnectionInfoes = new HashSet<ConnectionInfo>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string Comments { get; set; }
        public bool IsFastChargeCapable { get; set; }
        public int? DisplayOrder { get; set; }

        public virtual ICollection<ConnectionInfo> ConnectionInfoes { get; set; }
    }
}
