using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OCM.API.Common.Model
{
    public class User
    {
        public int ID { get; set; }

        [DisplayName("Identity Provider")]
        public string IdentityProvider { get; set; }

        [DisplayName("Identifier")]
        public string Identifier { get; set; }
        public string CurrentSessionToken { get; set; }

        [DisplayName("Your Name")]
        public string Username { get; set; }

        [DisplayName("Bio")]
        public string Profile { get; set; }

        [DisplayName("Location")]
        public string Location { get; set; }

        [DisplayName("Website"), ]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string WebsiteURL { get; set; }

        [DisplayName("Reputation Points")]
        public int? ReputationPoints { get; set; }

        public string Permissions { get; set; }
        public string PermissionsRequested { get; set; }

        [DisplayName("Date Joined")]
        public DateTime? DateCreated { get; set; }

        [DisplayName("Date Last Signed In")]
        public DateTime? DateLastLogin { get; set; }

        [DisplayName("Show Profile to Public")]
        public bool IsProfilePublic { get; set; }

        [DisplayName("Provide Emergency Charging")]
        public bool IsEmergencyChargingProvider { get; set; }

        [DisplayName("Provide Public Charging")]
        public bool IsPublicChargingProvider { get; set; }
        
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [DisplayName("Email Address (not public)")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        public string EmailAddress { get; set; }

        public bool IsCurrentSessionTokenValid {get;set;}

        public string APIKey { get; set; }

    }
}