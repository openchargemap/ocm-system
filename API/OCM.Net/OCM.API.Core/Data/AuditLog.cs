using System;

namespace OCM.Core.Data
{
    public partial class AuditLog
    {
        public int Id { get; set; }
        public DateTime EventDate { get; set; }
        public int UserId { get; set; }
        public string EventDescription { get; set; }
        public string Comment { get; set; }

        public virtual User User { get; set; }
    }
}
