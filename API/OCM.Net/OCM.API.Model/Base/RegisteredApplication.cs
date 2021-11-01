using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class RegisteredApplication : SimpleReferenceDataType
    {
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        [DisplayName("Website")]
        public string WebsiteURL { get; set; }
        public string Description { get; set; }

        [DisplayName("Enabled")]
        public bool IsEnabled { get; set; }

        [DisplayName("Enable Writes")]
        public bool IsWriteEnabled { get; set; }

        [DisplayName("List App in Public Showcase")]
        public bool IsPublicListing { get; set; }
        public string AppID { get; set; }

        [DisplayName("API Key")]
        public string PrimaryAPIKey { get; set; }
        public string DeprecatedAPIKey { get; set; }
        public string SharedSecret { get; set; }

        [DisplayName("Last Used")]
        public DateTime? DateAPIKeyLastUsed { get; set; }
        public DateTime DateAPIKeyUpdated { get; set; }

        [DisplayName("Created")]
        public DateTime DateCreated { get; set; }
        public int UserID { get; set; }

    }
}