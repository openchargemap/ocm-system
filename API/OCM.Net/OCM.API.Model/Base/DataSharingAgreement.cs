using System;
using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class DataSharingAgreement
    {
        public int ID { get; set; }
        public int UserID { get; set; }

        [Required]
        public string CompanyName { get; set; }

        [Required]
        public int CountryID { get; set; }

        [Required]
        public string RepresentativeName { get; set; }

        [Required]
        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        public string ContactEmail { get; set; }

        public bool? IsEmailVerified { get; set; }

        [Required]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string WebsiteURL { get; set; }

        [Required]
        public string DataFeedType { get; set; }

        [Required]
        public string DataFeedURL { get; set; }
        public string DistributionLimitations { get; set; }

        [Required]
        public string DataLicense { get; set; }
        public string Credentials { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime DateAgreed { get; set; }
        public DateTime DateAgreementTerminated { get; set; }

        public string Comments { get; set; }
    }
}