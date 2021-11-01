using System;

namespace OCM.Core.Data
{
    public partial class ViewAllEquipment
    {
        public int Id { get; set; }
        public int? DataProviderId { get; set; }
        public string DataProvider { get; set; }
        public string LocationTitle { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Town { get; set; }
        public string StateOrProvince { get; set; }
        public string Postcode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string ContactTelephone1 { get; set; }
        public string ContactTelephone2 { get; set; }
        public string ContactEmail { get; set; }
        public string AccessComments { get; set; }
        public string GeneralComments { get; set; }
        public string RelatedUrl { get; set; }
        public int? CountryId { get; set; }
        public string Country { get; set; }
        public string Isocode { get; set; }
        public string Usage { get; set; }
        public bool? IsPayAtLocation { get; set; }
        public bool? IsMembershipRequired { get; set; }
        public bool? IsAccessKeyRequired { get; set; }
        public int? OperatorId { get; set; }
        public string Operator { get; set; }
        public string WebsiteUrl { get; set; }
        public string Comments { get; set; }
        public string PhonePrimaryContact { get; set; }
        public string PhoneSecondaryContact { get; set; }
        public bool? IsPrivateIndividual { get; set; }
        public string BookingUrl { get; set; }
        public string DataProviderUrl { get; set; }
        public string DataProviderComments { get; set; }
        public string DataProvidersReference { get; set; }
        public string OperatorsReference { get; set; }
        public int? NumberOfPoints { get; set; }
        public string EquipmentGeneralComments { get; set; }
        public DateTime? DatePlanned { get; set; }
        public DateTime? DateLastConfirmed { get; set; }
        public DateTime? DateLastStatusUpdate { get; set; }
        public int? DataQualityLevel { get; set; }
        public DateTime? DateCreated { get; set; }
        public string SubmissionStatus { get; set; }
        public bool? IsLive { get; set; }
        public string EquipmentStatus { get; set; }
        public bool? IsOperational { get; set; }
        public string Connection1Type { get; set; }
    }
}
