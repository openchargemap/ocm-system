using System;

namespace OCM.API.Common.Model
{
    public enum OperatorProposalScope : short
    {
        CountrySpecific = 1,
        Global = 2
    }

    public enum OperatorProposalType : short
    {
        New = 1,
        Correction = 2
    }

    public enum OperatorProposalStatus : short
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Withdrawn = 4,
        Stale = 5
    }

    public class OperatorProposal
    {
        public int ID { get; set; }
        public int SubmittedByUserID { get; set; }
        public int? ReviewedByUserID { get; set; }
        public int? OperatorID { get; set; }
        public int? CountryID { get; set; }
        public OperatorProposalScope Scope { get; set; }
        public OperatorProposalType ProposalType { get; set; }
        public OperatorProposalStatus Status { get; set; }
        public string ProposedData { get; set; }
        public string PreviousData { get; set; }
        public string SubmitterComment { get; set; }
        public string DecisionComment { get; set; }
        public bool ConfirmWebsiteMatch { get; set; }
        public DateTime DateSubmitted { get; set; }
        public DateTime? DateReviewed { get; set; }
    }
}
