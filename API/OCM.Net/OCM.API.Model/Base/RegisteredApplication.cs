using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class RegisteredApplication : SimpleReferenceDataType
    {
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string WebsiteURL { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsWriteEnabled { get; set; }
        public string AppID { get; set; }
        public string PrimaryAPIKey { get; set; }
        public string DeprecatedAPIKey { get; set; }
        public string SharedSecret { get; set; }
        public DateTime? DateAPIKeyLastUsed { get; set; }
        public DateTime DateAPIKeyUpdated { get; set; }
        public DateTime DateCreated { get; set; }
        public int UserID { get; set; }

    }
}