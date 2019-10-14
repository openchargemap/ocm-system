using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class UserChargingRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime DateCreated { get; set; }
        public string Comment { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmergency { get; set; }

        public virtual User User { get; set; }
    }
}
