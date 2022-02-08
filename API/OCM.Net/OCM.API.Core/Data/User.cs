using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class User
    {
        public User()
        {
            AuditLogs = new HashSet<AuditLog>();
            DataSharingAgreements = new HashSet<DataSharingAgreement>();
            EditQueueItemProcessedByUsers = new HashSet<EditQueueItem>();
            EditQueueItemUsers = new HashSet<EditQueueItem>();
            MediaItems = new HashSet<MediaItem>();
            RegisteredApplicationUsers = new HashSet<RegisteredApplicationUser>();
            RegisteredApplications = new HashSet<RegisteredApplication>();
            Statistics = new HashSet<Statistic>();
            UserChargingRequests = new HashSet<UserChargingRequest>();
            UserComments = new HashSet<UserComment>();
            UserSubscriptions = new HashSet<UserSubscription>();
        }

        public int Id { get; set; }
        public string IdentityProvider { get; set; }
        public string Identifier { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string CurrentSessionToken { get; set; }
        public string Profile { get; set; }
        public string Location { get; set; }
        public string WebsiteUrl { get; set; }
        public int? ReputationPoints { get; set; }
        public string Permissions { get; set; }
        public string PermissionsRequested { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastLogin { get; set; }
        public string EmailAddress { get; set; }
        public bool IsEmergencyChargingProvider { get; set; }
        public bool? IsProfilePublic { get; set; }
        public bool IsPublicChargingProvider { get; set; }
        public string Apikey { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string SyncedSettings { get; set; }
        public bool IsReadOnly { get; set; }

        public virtual ICollection<AuditLog> AuditLogs { get; set; }
        public virtual ICollection<DataSharingAgreement> DataSharingAgreements { get; set; }
        public virtual ICollection<EditQueueItem> EditQueueItemProcessedByUsers { get; set; }
        public virtual ICollection<EditQueueItem> EditQueueItemUsers { get; set; }
        public virtual ICollection<MediaItem> MediaItems { get; set; }
        public virtual ICollection<RegisteredApplicationUser> RegisteredApplicationUsers { get; set; }
        public virtual ICollection<RegisteredApplication> RegisteredApplications { get; set; }
        public virtual ICollection<Statistic> Statistics { get; set; }
        public virtual ICollection<UserChargingRequest> UserChargingRequests { get; set; }
        public virtual ICollection<UserComment> UserComments { get; set; }
        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; }
    }
}
