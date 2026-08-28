using System.ComponentModel.DataAnnotations;

namespace OCM.Web.Models
{
    public class CountryOperatorEditModel
    {
        public int ID { get; set; }

        [Required]
        [Display(Name = "Country")]
        public int CountryID { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Operator name")]
        public string OperatorName { get; set; }

        [Required(ErrorMessage = "A website URL is required."), StringLength(500)]
        [DataType(DataType.Url)]
        [Display(Name = "Website")]
        public string WebsiteURL { get; set; }

        [Display(Name = "Notes")]
        public string Comments { get; set; }

        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Primary phone")]
        public string PhonePrimaryContact { get; set; }

        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Secondary phone")]
        public string PhoneSecondaryContact { get; set; }

        [DataType(DataType.EmailAddress)]
        [Display(Name = "Contact email")]
        public string ContactEmail { get; set; }

        [DataType(DataType.EmailAddress)]
        [Display(Name = "Fault-report email")]
        public string FaultReportEmail { get; set; }

        [Display(Name = "This is a separate operator")]
        public bool ConfirmNotDuplicate { get; set; }
    }
}
