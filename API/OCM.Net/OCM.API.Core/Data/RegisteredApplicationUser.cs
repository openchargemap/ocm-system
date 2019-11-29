using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class RegisteredApplicationUser
    {
        public int Id { get; set; }
        public int RegisteredApplicationId { get; set; }
        public int UserId { get; set; }
        public DateTime DateCreated { get; set; }
        public string Apikey { get; set; }
        public bool IsWriteEnabled { get; set; }
        public bool IsEnabled { get; set; }

        public virtual RegisteredApplication RegisteredApplication { get; set; }
        public virtual User User { get; set; }
    }
}
