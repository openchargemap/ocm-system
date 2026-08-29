using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.MVC.Controllers
{
    [Authorize(Roles = "StandardUser")]
    public class OperatorProposalsController : BaseController
    {
        private bool IsCountryEditor()
        {
            if (!UserID.HasValue) return false;

            var user = new UserManager().GetUser(UserID.Value);
            return user != null && !UserManager.IsUserAdministrator(user) &&
                new ReferenceDataManager().GetCountries(false)
                    .Any(country => UserManager.HasUserPermission(user, country.ID, PermissionLevel.Editor));
        }

        private void PopulateLists(OperatorProposalEditModel model)
        {
            var restrictToGlobal = IsCountryEditor();
            ViewBag.RestrictProposalScopeToGlobal = restrictToGlobal;
            ViewBag.ScopeList = new SelectList(restrictToGlobal ? new[]
            {
                new { Value = "2", Text = "Global / multinational" }
            } : new[]
            {
                new { Value = "1", Text = "Country-specific" },
                new { Value = "2", Text = "Global / multinational" }
            }, "Value", "Text", (int)model.Scope);
            ViewBag.CountryList = new SelectList(new ReferenceDataManager().GetCountries(false), "ID", "Title", model.CountryID);
            ViewBag.SelectedOperatorTitle = model.OperatorID.HasValue
                ? new OperatorInfoManager().GetOperatorInfo(model.OperatorID.Value)?.Title
                : null;
        }

        private void PopulateWebsiteMatch(OperatorProposalEditModel model)
        {
            if (string.IsNullOrWhiteSpace(model.WebsiteURL))
            {
                ViewBag.HasWebsiteMatch = false;
                return;
            }

            var countryIsoCode = model.Scope == OperatorProposalScope.CountrySpecific && model.CountryID.HasValue
                ? new ReferenceDataManager().GetCountries(false)
                    .FirstOrDefault(c => c.ID == model.CountryID.Value)?.ISOCode
                : null;
            var websiteHost = OperatorInfoManager.GetWebsiteHost(model.WebsiteURL);
            ViewBag.HasWebsiteMatch = !string.IsNullOrEmpty(websiteHost) &&
                new OperatorInfoManager().FindPotentialDuplicates(
                    GetProposedTitle(model), countryIsoCode, model.WebsiteURL, model.ContactEmail, model.OperatorID)
                    .Any(match => OperatorInfoManager.GetWebsiteHost(match.Operator?.WebsiteURL) == websiteHost);
        }

        private static string GetProposedTitle(OperatorProposalEditModel model)
        {
            var title = model.OperatorName?.Trim();
            if (model.Scope == OperatorProposalScope.CountrySpecific && model.CountryID.HasValue)
            {
                var country = new ReferenceDataManager().GetCountries(false).FirstOrDefault(c => c.ID == model.CountryID.Value);
                if (country != null) title += " (" + country.ISOCode.Trim().ToUpperInvariant() + ")";
            }
            return title;
        }

        private List<OperatorProposalListItem> BuildListItems(IEnumerable<OperatorProposal> proposals)
        {
            var countries = new ReferenceDataManager().GetCountries(false);
            return proposals.Select(proposal =>
            {
                var proposedOperator = string.IsNullOrWhiteSpace(proposal.ProposedData)
                    ? null
                    : JsonConvert.DeserializeObject<OperatorInfo>(proposal.ProposedData);
                var countryTitle = proposal.Scope == OperatorProposalScope.Global
                    ? "Global / multinational"
                    : countries.FirstOrDefault(c => c.ID == proposal.CountryID)?.Title ?? "Country not specified";

                return new OperatorProposalListItem
                {
                    Proposal = proposal,
                    OperatorName = proposedOperator?.Title ?? "Operator name unavailable",
                    CountryTitle = countryTitle
                };
            }).ToList();
        }

        private List<OperatorDuplicateMatchModel> FindDuplicateMatches(string title, OperatorProposalScope scope,
            int? countryId, string websiteUrl, string contactEmail, int? excludedId)
        {
            var manager = new OperatorInfoManager();
            var countryIsoCode = scope == OperatorProposalScope.CountrySpecific && countryId.HasValue
                ? new ReferenceDataManager().GetCountries(false).FirstOrDefault(c => c.ID == countryId.Value)?.ISOCode
                : null;
            return manager.FindPotentialDuplicates(title, countryIsoCode, websiteUrl, contactEmail, excludedId)
                .Take(5)
                .Select(match => new OperatorDuplicateMatchModel
                {
                    ID = match.Operator.ID,
                    Title = match.Operator.Title,
                    WebsiteURL = match.Operator.WebsiteURL,
                    MatchReason = match.Reason,
                    MatchType = match.MatchType,
                    ViewURL = Url.Action(nameof(Operator), new { id = match.Operator.ID })
                })
                .ToList();
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Add(OperatorProposalScope? scope)
        {
            return RenderSubmit(OperatorProposalType.New, scope);
        }

        [HttpGet]
        public ActionResult Edit(int? operatorId)
        {
            return RenderSubmit(OperatorProposalType.Correction, operatorId: operatorId);
        }

        [HttpGet]
        public ActionResult Submit(OperatorProposalType? proposalType)
        {
            return RedirectToAction(proposalType == OperatorProposalType.Correction ? nameof(Edit) : nameof(Add));
        }

        private ActionResult RenderSubmit(OperatorProposalType proposalType, OperatorProposalScope? scope = null, int? operatorId = null)
        {
            var restrictToGlobal = IsCountryEditor();
            var model = new OperatorProposalEditModel
            {
                ProposalType = proposalType,
                Scope = restrictToGlobal ? OperatorProposalScope.Global : scope ?? OperatorProposalScope.CountrySpecific
            };

            if (proposalType == OperatorProposalType.Correction && operatorId.HasValue)
            {
                var operatorInfo = new OperatorInfoManager().GetOperatorInfo(operatorId.Value);
                if (operatorInfo == null || operatorInfo.ID <= 1) return NotFound();

                var countryCode = OperatorInfoManager.GetCountryCodeFromTitle(operatorInfo.Title);
                var country = string.IsNullOrWhiteSpace(countryCode)
                    ? null
                    : new ReferenceDataManager().GetCountries(false)
                        .FirstOrDefault(item => string.Equals(item.ISOCode, countryCode, StringComparison.OrdinalIgnoreCase));
                model.Scope = country == null ? OperatorProposalScope.Global : OperatorProposalScope.CountrySpecific;
                model.CountryID = country?.ID;
                model.OperatorID = operatorInfo.ID;
                model.OperatorName = operatorInfo.Title;
                model.WebsiteURL = operatorInfo.WebsiteURL;
                model.BookingURL = operatorInfo.BookingURL;
                model.Comments = operatorInfo.Comments;
                model.PhonePrimaryContact = operatorInfo.PhonePrimaryContact;
                model.PhoneSecondaryContact = operatorInfo.PhoneSecondaryContact;
                model.ContactEmail = operatorInfo.ContactEmail;
                model.FaultReportEmail = operatorInfo.FaultReportEmail;
            }
            PopulateLists(model);
            PopulateWebsiteMatch(model);
            return View("Submit", model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Add(OperatorProposalEditModel model)
        {
            model.ProposalType = OperatorProposalType.New;
            return ProcessSubmit(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(OperatorProposalEditModel model)
        {
            model.ProposalType = OperatorProposalType.Correction;
            return ProcessSubmit(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Submit(OperatorProposalEditModel model)
        {
            return ProcessSubmit(model);
        }

        private ActionResult ProcessSubmit(OperatorProposalEditModel model)
        {
            if (IsCountryEditor())
            {
                model.Scope = OperatorProposalScope.Global;
                model.CountryID = null;
            }

            PopulateLists(model);
            PopulateWebsiteMatch(model);

            if (model.Scope == OperatorProposalScope.Global)
            {
                model.CountryID = null;
            }
            else if (!model.CountryID.HasValue)
            {
                ModelState.AddModelError(nameof(model.CountryID), "Please select a country.");
            }

            if (model.ProposalType == OperatorProposalType.New)
            {
                model.OperatorID = null;
            }
            else if (!model.OperatorID.HasValue)
            {
                ModelState.AddModelError(nameof(model.OperatorID), "Please select the operator being corrected.");
            }

            if (!ModelState.IsValid) return View("Submit", model);

            try
            {
                CheckForReadOnly();
                new OperatorProposalManager().Submit((int)UserID, model.Scope, model.ProposalType,
                    model.CountryID, model.OperatorID, new OperatorInfo
                    {
                        Title = model.OperatorName,
                        WebsiteURL = model.WebsiteURL,
                        Comments = model.Comments,
                        PhonePrimaryContact = model.PhonePrimaryContact,
                        PhoneSecondaryContact = model.PhoneSecondaryContact,
                        BookingURL = model.BookingURL,
                        ContactEmail = model.ContactEmail,
                        FaultReportEmail = model.FaultReportEmail
                    }, model.SubmitterComment, model.ConfirmWebsiteMatch);

                TempData["StatusMessage"] = "Your operator proposal was submitted for review.";
                return RedirectToAction(nameof(MySubmissions));
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                if (ex.Message.IndexOf("operator name", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ex.Message.IndexOf("operator with this name", StringComparison.OrdinalIgnoreCase) >= 0)
                    ModelState.AddModelError(nameof(model.OperatorName), ex.Message);
                return View("Submit", model);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public JsonResult CheckDuplicates(string operatorName, string websiteUrl, string contactEmail,
            OperatorProposalScope scope, int? countryId, int? operatorId)
        {
            var model = new OperatorProposalEditModel
            {
                OperatorName = operatorName,
                WebsiteURL = websiteUrl,
                ContactEmail = contactEmail,
                Scope = scope,
                CountryID = countryId,
                OperatorID = operatorId
            };

            return Json(new { matches = FindDuplicateMatches(GetProposedTitle(model), scope, countryId, websiteUrl, contactEmail, operatorId) });
        }

        [HttpGet]
        public JsonResult SearchOperators(string term, OperatorProposalScope scope, int? countryId, bool showAll = false)
        {
            if (string.IsNullOrWhiteSpace(term)) return Json(new { matches = Array.Empty<object>() });

            var operators = new OperatorInfoManager().GetOperators()
                .Where(o => o.ID > 1 && o.Title.IndexOf(term.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
            if (scope == OperatorProposalScope.CountrySpecific)
            {
                if (!countryId.HasValue) return Json(new { matches = Array.Empty<object>() });
                var country = new ReferenceDataManager().GetCountries(false).FirstOrDefault(c => c.ID == countryId.Value);
                if (country == null) return Json(new { matches = Array.Empty<object>() });
                var suffix = " (" + country.ISOCode.Trim().ToUpperInvariant() + ")";
                operators = operators.Where(o => o.Title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            }

            var total = operators.Count();
            var matches = (showAll ? operators : operators.Take(8)).Select(o => new
            {
                id = o.ID,
                title = o.Title,
                websiteURL = o.WebsiteURL,
                bookingURL = o.BookingURL,
                contactEmail = o.ContactEmail,
                faultReportEmail = o.FaultReportEmail,
                phonePrimaryContact = o.PhonePrimaryContact,
                phoneSecondaryContact = o.PhoneSecondaryContact,
                comments = o.Comments
            }).ToList();

            return Json(new
            {
                total,
                matches
            });
        }

        [HttpGet]
        public ActionResult Operator(int id)
        {
            return RedirectToAction("Details", "Operators", new { id });
        }

        [HttpGet]
        public ActionResult MySubmissions()
        {
            using var manager = new OperatorProposalManager();
            var proposals = BuildListItems(manager.GetSubmittedByUser((int)UserID));
            ViewBag.StatusMessage = TempData["StatusMessage"];
            return View("MyProposals", proposals);
        }

        [HttpGet]
        public ActionResult MyProposals()
        {
            return RedirectToAction(nameof(MySubmissions));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Withdraw(int id)
        {
            try
            {
                new OperatorProposalManager().Withdraw(id, (int)UserID);
                TempData["StatusMessage"] = "Your operator proposal was withdrawn.";
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(MySubmissions));
        }

        [HttpGet]
        public ActionResult Review()
        {
            using var manager = new OperatorProposalManager();
            return View(BuildListItems(manager.GetPending((int)UserID)));
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            using var manager = new OperatorProposalManager();
            var proposal = manager.GetPending((int)UserID).FirstOrDefault(p => p.ID == id);
            if (proposal == null) return Forbid();

            var proposedOperator = JsonConvert.DeserializeObject<OperatorInfo>(proposal.ProposedData);

            return View(new OperatorProposalReviewModel
            {
                Proposal = proposal,
                ProposedOperator = proposedOperator,
                CurrentOperator = proposal.OperatorID.HasValue ? new OperatorInfoManager().GetOperatorInfo(proposal.OperatorID.Value) : null,
                PotentialDuplicates = FindDuplicateMatches(
                    proposedOperator?.Title,
                    (OperatorProposalScope)proposal.Scope,
                    proposal.CountryID,
                    proposedOperator?.WebsiteURL,
                    proposedOperator?.ContactEmail,
                    proposal.OperatorID),
                CountryTitle = proposal.CountryID.HasValue ? new ReferenceDataManager().GetCountries(false).FirstOrDefault(c => c.ID == proposal.CountryID.Value)?.Title : "Global / multinational",
                SubmitterName = new UserManager().GetUser(proposal.SubmittedByUserID)?.Username
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Approve(int id, string decisionComment)
        {
            try
            {
                CheckForReadOnly();
                new OperatorProposalManager().Approve(id, (int)UserID, decisionComment);
                TempData["StatusMessage"] = "The operator proposal was approved.";
                return RedirectToAction(nameof(Review));
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Reject(int id, string decisionComment)
        {
            try
            {
                CheckForReadOnly();
                new OperatorProposalManager().Reject(id, (int)UserID, decisionComment);
                TempData["StatusMessage"] = "The operator proposal was rejected.";
                return RedirectToAction(nameof(Review));
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}
