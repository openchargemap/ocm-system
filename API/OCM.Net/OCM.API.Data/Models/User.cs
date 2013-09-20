using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class User
    {
        public User()
        {
            this.AuditLogs = new List<AuditLog>();
            this.ChargePoints = new List<ChargePoint>();
            this.DataProviderUsers = new List<DataProviderUser>();
            this.EditQueueItems = new List<EditQueueItem>();
            this.EditQueueItems1 = new List<EditQueueItem>();
            this.MediaItems = new List<MediaItem>();
            this.UserComments = new List<UserComment>();
        }

        public int ID { get; set; }
        public string IdentityProvider { get; set; }
        public string Identifier { get; set; }
        public string Username { get; set; }
        public string CurrentSessionToken { get; set; }
        public string Profile { get; set; }
        public string Location { get; set; }
        public string WebsiteURL { get; set; }
        public Nullable<int> ReputationPoints { get; set; }
        public string Permissions { get; set; }
        public string PermissionsRequested { get; set; }
        public System.DateTime DateCreated { get; set; }
        public Nullable<System.DateTime> DateLastLogin { get; set; }
        public string EmailAddress { get; set; }
        public bool IsEmergencyChargingProvider { get; set; }
        public bool IsProfilePublic { get; set; }
        public bool IsPublicChargingProvider { get; set; }
        public string APIKey { get; set; }
        public Nullable<double> Latitude { get; set; }
        public Nullable<double> Longitude { get; set; }
        public virtual ICollection<AuditLog> AuditLogs { get; set; }
        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
        public virtual ICollection<DataProviderUser> DataProviderUsers { get; set; }
        public virtual ICollection<EditQueueItem> EditQueueItems { get; set; }
        public virtual ICollection<EditQueueItem> EditQueueItems1 { get; set; }
        public virtual ICollection<MediaItem> MediaItems { get; set; }
        public virtual ICollection<UserComment> UserComments { get; set; }
    }
}
