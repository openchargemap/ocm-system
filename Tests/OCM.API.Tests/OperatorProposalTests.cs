using OCM.API.Common;
using OCM.API.Common.Model;
using Xunit;

namespace OCM.API.Tests
{
    public class OperatorProposalTests
    {
        [Fact]
        public void Proposal_statuses_have_distinct_lifecycle_values()
        {
            var statuses = new[]
            {
                OperatorProposalStatus.Pending,
                OperatorProposalStatus.Approved,
                OperatorProposalStatus.Rejected,
                OperatorProposalStatus.Withdrawn,
                OperatorProposalStatus.Stale
            };

            Assert.Equal(statuses.Length, new System.Collections.Generic.HashSet<OperatorProposalStatus>(statuses).Count);
        }

        [Fact]
        public void Possible_duplicates_require_confirmation()
        {
            var match = new OperatorMatch { MatchType = OperatorMatchType.PossibleDuplicate };

            Assert.True(match.RequiresConfirmation);
        }

        [Theory]
        [InlineData(OperatorMatchType.DuplicateTitle)]
        [InlineData(OperatorMatchType.OtherCountry)]
        public void Non_confirmable_matches_do_not_require_confirmation(OperatorMatchType matchType)
        {
            var match = new OperatorMatch { MatchType = matchType };

            Assert.False(match.RequiresConfirmation);
        }
    }
}
