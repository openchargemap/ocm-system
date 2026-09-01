using System;

namespace OCM.Core.Data
{
    public partial class OperatorProposal
    {
        public int Id { get; set; }
        public int SubmittedByUserId { get; set; }
        public int? ReviewedByUserId { get; set; }
        public int? OperatorId { get; set; }
        public int? CountryId { get; set; }
        public short Scope { get; set; }
        public short ProposalType { get; set; }
        public short Status { get; set; }
        public string ProposedData { get; set; }
        public string PreviousData { get; set; }
        public string SubmitterComment { get; set; }
        public string DecisionComment { get; set; }
        public bool ConfirmWebsiteMatch { get; set; }
        public DateTime DateSubmitted { get; set; }
        public DateTime? DateReviewed { get; set; }
    }
}
