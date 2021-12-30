using System;
using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class EditQueueFilter
    {
        [Display(Name = "Show Edits Only")]
        public bool ShowEditsOnly { get; set; }

        [Display(Name = "Show Processed Items")]
        public bool ShowProcessed { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        [Display(Name = "Date From")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? DateFrom { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        [Display(Name = "Date To")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? DateTo { get; set; }

        public int MinimumDifferences { get; set; }
        public int MaxResults { get; set; }

        [Display(Name = "OCM ID")]
        public int? ID { get; set; }


        [Display(Name = "User ID")]
        public int? UserId { get; set; }

        public EditQueueFilter()
        {
            MinimumDifferences = 1;
            MaxResults = 200;
            ShowEditsOnly = true;
        }
    }
}