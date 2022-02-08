using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class DataSharingAgreement
    {
        public int Id { get; set; }
        /// <summary>
        /// Organisation making agreement
        /// </summary>
        public string CompanyName { get; set; }
        public int CountryId { get; set; }
        /// <summary>
        /// Name of person authorised by organisation to make agreement
        /// </summary>
        public string RepresentativeName { get; set; }
        public string ContactEmail { get; set; }
        public bool IsEmailVerified { get; set; }
        /// <summary>
        /// Website of data provider
        /// </summary>
        public string WebsiteUrl { get; set; }
        public string DataFeedType { get; set; }
        public string DataFeedUrl { get; set; }
        public string DistributionLimitations { get; set; }
        public string DataLicense { get; set; }
        public string Credentials { get; set; }
        /// <summary>
        /// OCM User ID of user who completed agreement
        /// </summary>
        public int UserId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateAgreed { get; set; }
        public DateTime? DateAgreementTerminated { get; set; }
        public string Comments { get; set; }

        public virtual Country Country { get; set; }
        public virtual User User { get; set; }
    }
}
