using Newtonsoft.Json;
using OCM.API.Common.Model;
using OCM.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using DataCountry = OCM.Core.Data.Country;
using UserModel = OCM.API.Common.Model.User;
using ProposalModel = OCM.API.Common.Model.OperatorProposal;

namespace OCM.API.Common
{
    public class OperatorProposalManager : ManagerBase
    {
        private static readonly JsonSerializerSettings ProposalSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public ProposalModel Submit(int userId, OperatorProposalScope scope, OperatorProposalType proposalType,
            int? countryId, int? operatorId, OperatorInfo proposedOperator, string submitterComment,
            bool confirmWebsiteMatch)
        {
            var user = new UserManager().GetUser(userId);
            if (user == null) throw new InvalidOperationException("The submitting user could not be found.");

            var country = ValidateScope(user, scope, countryId);
            var target = ValidateTarget(scope, proposalType, countryId, operatorId);
            var proposed = BuildProposedOperator(scope, country, proposedOperator);
            ValidateDuplicates(proposed, operatorId, confirmWebsiteMatch, scope, countryId);

            var entity = new Core.Data.OperatorProposal
            {
                SubmittedByUserId = userId,
                Scope = (short)scope,
                ProposalType = (short)proposalType,
                Status = (short)OperatorProposalStatus.Pending,
                OperatorId = operatorId,
                CountryId = countryId,
                ProposedData = JsonConvert.SerializeObject(proposed, ProposalSerializerSettings),
                PreviousData = target == null ? null : JsonConvert.SerializeObject(ToModel(target), ProposalSerializerSettings),
                SubmitterComment = submitterComment,
                ConfirmWebsiteMatch = confirmWebsiteMatch,
                DateSubmitted = DateTime.UtcNow
            };

            dataModel.OperatorProposals.Add(entity);
            dataModel.SaveChanges();

            AuditLogManager.Log(user, AuditEventType.CreatedItem,
                "{EntityType:\"OperatorProposal\",EntityID:" + entity.Id + "}",
                $"User submitted operator proposal {entity.Id}");

            return ToModel(entity);
        }

        public List<ProposalModel> GetPending(int userId)
        {
            var user = new UserManager().GetUser(userId);
            if (user == null) return new List<ProposalModel>();

            var isAdmin = UserManager.IsUserAdministrator(user);
            var countryIds = new ReferenceDataManager().GetCountries(false)
                .Where(c => UserManager.HasUserPermission(user, c.ID, PermissionLevel.Editor))
                .Select(c => c.ID)
                .ToList();

            return dataModel.OperatorProposals
                .Where(p => p.Status == (short)OperatorProposalStatus.Pending &&
                    p.SubmittedByUserId != userId &&
                    (isAdmin || (p.Scope == (short)OperatorProposalScope.CountrySpecific && p.CountryId.HasValue && countryIds.Contains(p.CountryId.Value))))
                .OrderByDescending(p => p.DateSubmitted)
                .AsEnumerable()
                .Select(ToModel)
                .ToList();
        }

        public List<ProposalModel> GetSubmittedByUser(int userId)
        {
            return dataModel.OperatorProposals
                .Where(p => p.SubmittedByUserId == userId)
                .OrderByDescending(p => p.DateSubmitted)
                .AsEnumerable()
                .Select(ToModel)
                .ToList();
        }

        public ProposalModel Approve(int proposalId, int reviewerId, string decisionComment)
        {
            var proposal = GetPendingEntity(proposalId);
            var reviewer = RequireReviewer(proposal, reviewerId);
            var proposed = Deserialize(proposal.ProposedData);
            var target = proposal.OperatorId.HasValue ? dataModel.Operators.FirstOrDefault(o => o.Id == proposal.OperatorId.Value) : null;

            if (proposal.ProposalType == (short)OperatorProposalType.Correction)
            {
                if (target == null || !SnapshotsMatch(proposal.PreviousData, target))
                {
                    MarkStale(proposal, reviewer, "The operator changed after this proposal was submitted.");
                    throw new InvalidOperationException("This proposal is stale because the operator has changed.");
                }
            }

            ValidateDuplicates(proposed, proposal.OperatorId, proposal.ConfirmWebsiteMatch,
                (OperatorProposalScope)proposal.Scope, proposal.CountryId, proposal.Id);

            if (target == null)
            {
                target = new Core.Data.Operator();
                dataModel.Operators.Add(target);
            }
            Apply(target, proposed);
            proposal.Status = (short)OperatorProposalStatus.Approved;
            proposal.ReviewedByUserId = reviewerId;
            proposal.DateReviewed = DateTime.UtcNow;
            proposal.DecisionComment = decisionComment;
            dataModel.SaveChanges();

            AuditLogManager.Log(reviewer, AuditEventType.UpdatedItem,
                "{EntityType:\"OperatorProposal\",EntityID:" + proposal.Id + ",OperatorID:" + target.Id + "}",
                $"User approved operator proposal {proposal.Id}");
            CacheManager.RefreshCachedData();
            return ToModel(proposal);
        }

        public ProposalModel Reject(int proposalId, int reviewerId, string decisionComment)
        {
            if (string.IsNullOrWhiteSpace(decisionComment))
                throw new InvalidOperationException("A rejection reason is required.");

            var proposal = GetPendingEntity(proposalId);
            var reviewer = RequireReviewer(proposal, reviewerId);
            proposal.Status = (short)OperatorProposalStatus.Rejected;
            proposal.ReviewedByUserId = reviewerId;
            proposal.DateReviewed = DateTime.UtcNow;
            proposal.DecisionComment = decisionComment;
            dataModel.SaveChanges();

            AuditLogManager.Log(reviewer, AuditEventType.UpdatedItem,
                "{EntityType:\"OperatorProposal\",EntityID:" + proposal.Id + "}",
                $"User rejected operator proposal {proposal.Id}");
            return ToModel(proposal);
        }

        public ProposalModel Withdraw(int proposalId, int submitterId)
        {
            var proposal = GetPendingEntity(proposalId);
            if (proposal.SubmittedByUserId != submitterId)
                throw new UnauthorizedAccessException("You can only withdraw your own proposal.");

            proposal.Status = (short)OperatorProposalStatus.Withdrawn;
            proposal.DateReviewed = DateTime.UtcNow;
            proposal.DecisionComment = "Withdrawn by submitter.";
            dataModel.SaveChanges();

            var submitter = new UserManager().GetUser(submitterId);
            AuditLogManager.Log(submitter, AuditEventType.UpdatedItem,
                "{EntityType:\"OperatorProposal\",EntityID:" + proposal.Id + "}",
                $"User withdrew operator proposal {proposal.Id}");
            return ToModel(proposal);
        }

        private DataCountry ValidateScope(UserModel user, OperatorProposalScope scope, int? countryId)
        {
            if (scope == OperatorProposalScope.Global)
            {
                if (countryId.HasValue) throw new InvalidOperationException("Global proposals cannot specify a country.");
                return null;
            }

            if (!countryId.HasValue)
                throw new InvalidOperationException("A country is required for a country-specific proposal.");

            var country = dataModel.Countries.FirstOrDefault(c => c.Id == countryId.Value);
            if (country == null) throw new InvalidOperationException("Unknown country.");
            return country;
        }

        private Core.Data.Operator ValidateTarget(OperatorProposalScope scope, OperatorProposalType type, int? countryId, int? operatorId)
        {
            if (type == OperatorProposalType.New && operatorId.HasValue)
                throw new InvalidOperationException("A new operator proposal cannot target an existing operator.");
            if (type == OperatorProposalType.Correction && !operatorId.HasValue)
                throw new InvalidOperationException("A correction proposal must target an existing operator.");

            var target = operatorId.HasValue ? dataModel.Operators.FirstOrDefault(o => o.Id == operatorId.Value) : null;
            if (type == OperatorProposalType.Correction && target == null)
                throw new InvalidOperationException("The target operator could not be found.");
            if (scope == OperatorProposalScope.CountrySpecific && target != null &&
                !target.Title.EndsWith(" (" + dataModel.Countries.First(c => c.Id == countryId.Value).Isocode + ")", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The target operator is not specific to the selected country.");
            return target;
        }

        private static OperatorInfo BuildProposedOperator(OperatorProposalScope scope, DataCountry country, OperatorInfo proposed)
        {
            if (proposed == null || string.IsNullOrWhiteSpace(proposed.Title))
                throw new InvalidOperationException("An operator name is required.");

            var result = proposed;
            result.ID = 0;
            if (scope == OperatorProposalScope.CountrySpecific)
            {
                var suffix = " (" + country.Isocode.Trim().ToUpperInvariant() + ")";
                var name = result.Title.Trim();
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - suffix.Length).Trim();
                result.Title = name + suffix;
            }
            else result.Title = result.Title.Trim();
            return result;
        }

        private void ValidateDuplicates(OperatorInfo proposed, int? excludedId, bool confirmWebsiteMatch,
            OperatorProposalScope scope, int? countryId, int? excludedProposalId = null)
        {
            var countryIsoCode = scope == OperatorProposalScope.CountrySpecific && countryId.HasValue
                ? dataModel.Countries.First(c => c.Id == countryId.Value).Isocode
                : null;
            var matches = new OperatorInfoManager().FindPotentialDuplicates(
                proposed.Title, countryIsoCode, proposed.WebsiteURL, proposed.ContactEmail, excludedId);
            if (matches.Any(m => m.MatchType == OperatorMatchType.DuplicateTitle))
                throw new InvalidOperationException("An operator with this name already exists.");
            var proposedWebsiteHost = OperatorInfoManager.GetWebsiteHost(proposed.WebsiteURL);
            if (!string.IsNullOrEmpty(proposedWebsiteHost) &&
                matches.Any(m => m.Operator != null &&
                    OperatorInfoManager.GetWebsiteHost(m.Operator.WebsiteURL) == proposedWebsiteHost) && !confirmWebsiteMatch)
                throw new InvalidOperationException("An operator with the same website already exists. Confirm this is not a duplicate.");

            var hasPendingMatch = dataModel.OperatorProposals
                .Where(p => p.Status == (short)OperatorProposalStatus.Pending &&
                    (!excludedProposalId.HasValue || p.Id != excludedProposalId.Value) &&
                    p.Scope == (short)scope && p.CountryId == countryId)
                .AsEnumerable()
                .Select(p => Deserialize(p.ProposedData))
                .Any(p => p != null && OperatorInfoManager.NormalizeTitle(p.Title) == OperatorInfoManager.NormalizeTitle(proposed.Title));
            if (hasPendingMatch)
                throw new InvalidOperationException("A pending proposal with this operator name already exists for the selected scope.");
        }

        private Core.Data.OperatorProposal GetPendingEntity(int id)
        {
            var proposal = dataModel.OperatorProposals.FirstOrDefault(p => p.Id == id && p.Status == (short)OperatorProposalStatus.Pending);
            if (proposal == null) throw new InvalidOperationException("The proposal is no longer pending.");
            return proposal;
        }

        private UserModel RequireReviewer(Core.Data.OperatorProposal proposal, int reviewerId)
        {
            var reviewer = new UserManager().GetUser(reviewerId);
            if (reviewer == null || (proposal.Scope == (short)OperatorProposalScope.Global && !UserManager.IsUserAdministrator(reviewer)) ||
                (proposal.Scope == (short)OperatorProposalScope.CountrySpecific && !UserManager.HasUserPermission(reviewer, proposal.CountryId, PermissionLevel.Editor)))
                throw new UnauthorizedAccessException("You are not authorized to review this proposal.");
            if (proposal.SubmittedByUserId == reviewerId)
                throw new UnauthorizedAccessException("You cannot review your own proposal.");
            return reviewer;
        }

        private void MarkStale(Core.Data.OperatorProposal proposal, UserModel reviewer, string comment)
        {
            proposal.Status = (short)OperatorProposalStatus.Stale;
            proposal.ReviewedByUserId = reviewer.ID;
            proposal.DateReviewed = DateTime.UtcNow;
            proposal.DecisionComment = comment;
            dataModel.SaveChanges();
        }

        private static bool SnapshotsMatch(string previousData, Core.Data.Operator current)
        {
            if (string.IsNullOrWhiteSpace(previousData)) return false;
            var previous = Deserialize(previousData);
            return previous != null && OperatorInfoManager.NormalizeTitle(previous.Title) == OperatorInfoManager.NormalizeTitle(current.Title) &&
                previous.WebsiteURL == current.WebsiteUrl && previous.Comments == current.Comments &&
                previous.PhonePrimaryContact == current.PhonePrimaryContact && previous.PhoneSecondaryContact == current.PhoneSecondaryContact &&
                previous.BookingURL == current.BookingUrl && previous.ContactEmail == current.ContactEmail && previous.FaultReportEmail == current.FaultReportEmail;
        }

        private static OperatorInfo Deserialize(string json) => string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<OperatorInfo>(json);

        private static OperatorInfo ToModel(Core.Data.Operator source) => source == null ? null : new OperatorInfo
        {
            ID = source.Id,
            Title = source.Title,
            WebsiteURL = source.WebsiteUrl,
            Comments = source.Comments,
            PhonePrimaryContact = source.PhonePrimaryContact,
            PhoneSecondaryContact = source.PhoneSecondaryContact,
            BookingURL = source.BookingUrl,
            ContactEmail = source.ContactEmail,
            FaultReportEmail = source.FaultReportEmail,
            IsPrivateIndividual = source.IsPrivateIndividual,
            IsRestrictedEdit = source.IsRestrictedEdit
        };

        private static ProposalModel ToModel(Core.Data.OperatorProposal source) => source == null ? null : new ProposalModel
        {
            ID = source.Id,
            SubmittedByUserID = source.SubmittedByUserId,
            ReviewedByUserID = source.ReviewedByUserId,
            OperatorID = source.OperatorId,
            CountryID = source.CountryId,
            Scope = (OperatorProposalScope)source.Scope,
            ProposalType = (OperatorProposalType)source.ProposalType,
            Status = (OperatorProposalStatus)source.Status,
            ProposedData = source.ProposedData,
            PreviousData = source.PreviousData,
            SubmitterComment = source.SubmitterComment,
            DecisionComment = source.DecisionComment,
            ConfirmWebsiteMatch = source.ConfirmWebsiteMatch,
            DateSubmitted = source.DateSubmitted,
            DateReviewed = source.DateReviewed
        };

        private static void Apply(Core.Data.Operator target, OperatorInfo source)
        {
            target.Title = source.Title;
            target.WebsiteUrl = source.WebsiteURL;
            target.Comments = source.Comments;
            target.PhonePrimaryContact = source.PhonePrimaryContact;
            target.PhoneSecondaryContact = source.PhoneSecondaryContact;
            target.BookingUrl = source.BookingURL;
            target.ContactEmail = source.ContactEmail;
            target.FaultReportEmail = source.FaultReportEmail;
        }
    }
}
