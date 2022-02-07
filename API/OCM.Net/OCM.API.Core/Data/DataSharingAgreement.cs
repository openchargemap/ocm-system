using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class DataSharingAgreement
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string RepresentativeName { get; set; }
        public string ContactEmail { get; set; }
        public bool IsEmailVerified { get; set; }
        public string WebsiteUrl { get; set; }
        public string DataFeedType { get; set; }
        public string DataFeedUrl { get; set; }
        public string Credentials { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateAgreed { get; set; }
    }
}
