using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class UserSubscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public int? CountryId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? DistanceKm { get; set; }
        public string FilterSettings { get; set; }
        public DateTime? DateLastNotified { get; set; }
        public bool? IsEnabled { get; set; }
        public DateTime DateCreated { get; set; }
        public bool NotifyPoiadditions { get; set; }
        public bool NotifyPoiedits { get; set; }
        public bool NotifyPoiupdates { get; set; }
        public bool NotifyComments { get; set; }
        public bool NotifyMedia { get; set; }
        public bool NotifyEmergencyChargingRequests { get; set; }
        public bool NotifyGeneralChargingRequests { get; set; }
        public int NotificationFrequencyMins { get; set; }

        public virtual Country Country { get; set; }
        public virtual User User { get; set; }
    }
}
