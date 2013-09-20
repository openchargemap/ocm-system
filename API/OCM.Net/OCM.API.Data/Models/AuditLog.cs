using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class AuditLog
    {
        public int ID { get; set; }
        public System.DateTime EventDate { get; set; }
        public int UserID { get; set; }
        public string EventDescription { get; set; }
        public string Comment { get; set; }
        public virtual User User { get; set; }
    }
}
