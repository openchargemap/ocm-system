using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class RegisteredApplication
    {
        public RegisteredApplication()
        {
            RegisteredApplicationUsers = new HashSet<RegisteredApplicationUser>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string WebsiteUrl { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsWriteEnabled { get; set; }
        public bool IsPublicListing { get; set; }
        public string AppId { get; set; }
        public string PrimaryApikey { get; set; }
        public string DeprecatedApikey { get; set; }
        public string SharedSecret { get; set; }
        public DateTime? DateApikeyLastUsed { get; set; }
        public DateTime DateCreated { get; set; }
        public int UserId { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<RegisteredApplicationUser> RegisteredApplicationUsers { get; set; }
    }
}
