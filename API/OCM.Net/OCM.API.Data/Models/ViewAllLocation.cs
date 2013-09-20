using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class ViewAllLocation
    {
        public int ID { get; set; }
        public string DataProvider { get; set; }
        public string LocationTitle { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Town { get; set; }
        public string StateOrProvince { get; set; }
        public string Postcode { get; set; }
        public Nullable<double> Latitude { get; set; }
        public Nullable<double> Longitude { get; set; }
        public string ContactTelephone1 { get; set; }
        public string ContactTelephone2 { get; set; }
        public string ContactEmail { get; set; }
        public string AccessComments { get; set; }
        public string GeneralComments { get; set; }
        public string RelatedURL { get; set; }
        public string Country { get; set; }
        public string ISOCode { get; set; }
        public string Usage { get; set; }
        public Nullable<bool> IsPayAtLocation { get; set; }
        public Nullable<bool> IsMembershipRequired { get; set; }
        public Nullable<bool> IsAccessKeyRequired { get; set; }
        public string Operator { get; set; }
        public string WebsiteURL { get; set; }
        public string Comments { get; set; }
        public string PhonePrimaryContact { get; set; }
        public string PhoneSecondaryContact { get; set; }
        public Nullable<bool> IsPrivateIndividual { get; set; }
        public string BookingURL { get; set; }
        public string DataProviderURL { get; set; }
        public string DataProviderComments { get; set; }
        public string DataProvidersReference { get; set; }
        public string OperatorsReference { get; set; }
        public Nullable<int> NumberOfPoints { get; set; }
        public string EquipmentGeneralComments { get; set; }
        public Nullable<System.DateTime> DatePlanned { get; set; }
        public Nullable<System.DateTime> DateLastConfirmed { get; set; }
        public Nullable<System.DateTime> DateLastStatusUpdate { get; set; }
        public Nullable<int> DataQualityLevel { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public string SubmissionStatus { get; set; }
        public Nullable<bool> IsLive { get; set; }
        public string EquipmentStatus { get; set; }
        public Nullable<bool> IsOperational { get; set; }
    }
}
