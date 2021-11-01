using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class UserSubscription
    {
        public int ID { get; set; }

        public int UserID { get; set; }

        [DisplayName("Subscription Title"), Required]
        public string Title { get; set; }

        [DisplayName("Country")]
        public Nullable<int> CountryID { get; set; }

        public Country Country { get; set; }

        [Range(-90, 90)]
        public Nullable<double> Latitude { get; set; }

        [Range(-180, 180)]
        public Nullable<double> Longitude { get; set; }

        [Range(0, 100000)]
        [DisplayName("Distance (KM)")]
        public Nullable<double> DistanceKM { get; set; }

        public UserSubscriptionFilter FilterSettings { get; set; }

        [DisplayName("Date Last Notified")]
        public Nullable<System.DateTime> DateLastNotified { get; set; }

        [DisplayName("Enable This Subscription"), Required]
        public bool IsEnabled { get; set; }

        [DisplayName("Date Created")]
        public System.DateTime DateCreated { get; set; }

        [DisplayName("New locations are added"), Required]
        public bool NotifyPOIAdditions { get; set; }

        [DisplayName("Locations edits are awaiting approval"), Required]
        public bool NotifyPOIEdits { get; set; }

        [DisplayName("Locations updates are published"), Required]
        public bool NotifyPOIUpdates { get; set; }

        [DisplayName("New comments/check-ins are added"), Required]
        public bool NotifyComments { get; set; }

        [DisplayName("New photos/media items added"), Required]
        public bool NotifyMedia { get; set; }

        [DisplayName("Emergency charging requested in area"), Required]
        public bool NotifyEmergencyChargingRequests { get; set; }

        [DisplayName("General charging requested in area"), Required]
        public bool NotifyGeneralChargingRequests { get; set; }

        [DisplayName("Notification Frequency"), Required]
        public int NotificationFrequencyMins { get; set; }
    }

    public class UserSubscriptionFilter
    {
        public List<int> ConnectionTypeIDs { get; set; }

        public List<int> OperatorIDs { get; set; }

        public List<int> LevelIDs { get; set; }

        public List<int> UsageTypeIDs { get; set; }

        public List<int> StatusTypeIDs { get; set; }

        public UserSubscriptionFilter()
        {
            ConnectionTypeIDs = new List<int>();
            OperatorIDs = new List<int>();
            LevelIDs = new List<int>();
            UsageTypeIDs = new List<int>();
            StatusTypeIDs = new List<int>();
        }
    }

}