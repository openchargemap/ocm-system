using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OCM.API.Common;
using OCM.API.Common.Model;

namespace OCM.Web.Models
{
    public class OperatorDuplicateMatchModel
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string WebsiteURL { get; set; }
        public string MatchReason { get; set; }
        public OperatorMatchType MatchType { get; set; }
        public string ViewURL { get; set; }
    }

    public class OperatorProposalEditModel
    {
        [Required]
        [Display(Name = "Proposal scope")]
        public OperatorProposalScope Scope { get; set; } = OperatorProposalScope.CountrySpecific;

        [Display(Name = "Country")]
        public int? CountryID { get; set; }

        [Required]
        [Display(Name = "Proposal type")]
        public OperatorProposalType ProposalType { get; set; } = OperatorProposalType.New;

        [Display(Name = "Existing operator")]
        public int? OperatorID { get; set; }

        [Required(ErrorMessage = "Please enter the operator name."), StringLength(250)]
        [Display(Name = "Operator name")]
        public string OperatorName { get; set; }

        [Url, StringLength(500)]
        [Display(Name = "Website")]
        public string WebsiteURL { get; set; }

        [StringLength(500)]
        [Display(Name = "Operator comments")]
        public string Comments { get; set; }

        [Phone, StringLength(100)]
        [Display(Name = "Primary phone")]
        public string PhonePrimaryContact { get; set; }

        [Phone, StringLength(100)]
        [Display(Name = "Secondary phone")]
        public string PhoneSecondaryContact { get; set; }

        [Url, StringLength(500)]
        [Display(Name = "Booking URL")]
        public string BookingURL { get; set; }

        [EmailAddress, StringLength(500)]
        [Display(Name = "Contact email")]
        public string ContactEmail { get; set; }

        [EmailAddress, StringLength(500)]
        [Display(Name = "Fault-report email")]
        public string FaultReportEmail { get; set; }

        [Display(Name = "Confirm website match")]
        public bool ConfirmWebsiteMatch { get; set; }

        [StringLength(2000)]
        [Display(Name = "Proposal rationale")]
        public string SubmitterComment { get; set; }
    }

    public class OperatorProposalReviewModel
    {
        public OperatorProposal Proposal { get; set; }
        public OperatorInfo ProposedOperator { get; set; }
        public OperatorInfo CurrentOperator { get; set; }
        public string CountryTitle { get; set; }
        public string SubmitterName { get; set; }
        public List<DiffItem> Differences { get; set; } = new List<DiffItem>();
        public List<OperatorDuplicateMatchModel> PotentialDuplicates { get; set; } = new List<OperatorDuplicateMatchModel>();

        [StringLength(2000)]
        [Display(Name = "Decision comment")]
        public string DecisionComment { get; set; }
    }

    public class OperatorProposalListItem
    {
        public OperatorProposal Proposal { get; set; }
        public string OperatorName { get; set; }
        public string CountryTitle { get; set; }
    }
}
