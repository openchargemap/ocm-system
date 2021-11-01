using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class OperatorInfo : SimpleReferenceDataType
    {
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string WebsiteURL { get; set; }
        public string Comments { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.PhoneNumber)]
        public string PhonePrimaryContact { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.PhoneNumber)]
        public string PhoneSecondaryContact { get; set; }
        public bool? IsPrivateIndividual { get; set; }
        public AddressInfo AddressInfo { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string BookingURL { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        public string ContactEmail { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        public string FaultReportEmail { get; set; }
        public bool? IsRestrictedEdit { get; set; }
    }
}