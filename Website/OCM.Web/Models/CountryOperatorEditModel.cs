using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OCM.Web.Models
{
    public class CountryOperatorListItem
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string CountryTitle { get; set; }
        public string WebsiteURL { get; set; }
    }

    public class CountryOperatorIndexModel
    {
        public int? CountryID { get; set; }
        public string SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalResults { get; set; }
        public List<CountryOperatorListItem> Operators { get; set; } = new List<CountryOperatorListItem>();

        public bool HasCountry => CountryID.HasValue;
        public int TotalPages => TotalResults == 0 ? 0 : (TotalResults + PageSize - 1) / PageSize;
    }

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
