using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.API.Common.Model
{
    public class AuditLog
    {
        public int ID { get; set; }
        public DateTime EventDate { get; set; }
        public int UserID { get; set; }
        public string EventDescription { get; set; }
        public string Comment { get; set; }
    }
}