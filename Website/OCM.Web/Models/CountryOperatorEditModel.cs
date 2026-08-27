using System.ComponentModel.DataAnnotations;

namespace OCM.Web.Models
{
    public class CountryOperatorEditModel
    {
        public int ID { get; set; }

        [Required]
        public int CountryID { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Operator name")]
        public string OperatorName { get; set; }

        [DataType(DataType.Url)]
        public string WebsiteURL { get; set; }

        public string Comments { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string PhonePrimaryContact { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string PhoneSecondaryContact { get; set; }

        [DataType(DataType.Url)]
        public string BookingURL { get; set; }

        [DataType(DataType.EmailAddress)]
        public string ContactEmail { get; set; }

        [DataType(DataType.EmailAddress)]
        public string FaultReportEmail { get; set; }

        public bool ConfirmWebsiteMatch { get; set; }
    }
}
