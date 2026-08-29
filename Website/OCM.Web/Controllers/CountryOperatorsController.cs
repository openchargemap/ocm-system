using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.Web.Models;

namespace OCM.MVC.Controllers
{
    [Authorize(Roles = "StandardUser")]
    public class CountryOperatorsController : BaseController
    {
        private User GetCurrentUser()
        {
            return UserID.HasValue ? new UserManager().GetUser(UserID.Value) : null;
        }

        private List<Country> GetEditableCountries(User user)
        {
            if (user == null) return new List<Country>();

            var countries = new ReferenceDataManager().GetCountries(false);
            if (UserManager.IsUserAdministrator(user)) return countries;

            return countries
                .Where(c => UserManager.HasUserPermission(user, c.ID, PermissionLevel.Editor))
                .OrderBy(c => c.Title)
                .ToList();
        }

        private bool CanEditCountry(User user, int countryId)
        {
            return GetEditableCountries(user).Any(c => c.ID == countryId);
        }

        private void PopulateCountries(IEnumerable<Country> countries, int selectedCountryId)
        {
            var countryList = countries.ToList();
            ViewBag.CountryList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(countryList, "ID", "Title", selectedCountryId);
            ViewBag.SingleEditableCountry = countryList.Count == 1 ? countryList[0] : null;
        }

        [HttpGet]
        public ActionResult Index(int? countryId, string searchTerm, int page = 1)
        {
            var user = GetCurrentUser();
            var countries = GetEditableCountries(user);
            if (countries.Count == 0) return Forbid();

            var model = new CountryOperatorIndexModel
            {
                CountryID = countryId ?? (countries.Count == 1 ? countries[0].ID : (int?)null),
                SearchTerm = searchTerm?.Trim(),
                Page = Math.Max(1, page)
            };
            PopulateCountries(countries, model.CountryID ?? 0);

            if (!model.CountryID.HasValue)
                return View(model);

            if (!CanEditCountry(user, model.CountryID.Value)) return Forbid();

            if (!string.IsNullOrWhiteSpace(model.SearchTerm) && model.SearchTerm.Length < 2)
            {
                ViewBag.SearchTooShort = true;
                return View(model);
            }

            var country = countries.First(c => c.ID == model.CountryID.Value);
            var suffix = " (" + country.ISOCode + ")";
            var matches = new OperatorInfoManager().GetOperators()
                .Where(operatorInfo => operatorInfo.ID > 1 &&
                    operatorInfo.Title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
                matches = matches.Where(operatorInfo =>
                    operatorInfo.Title.IndexOf(model.SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0);

            var orderedMatches = matches
                .OrderBy(operatorInfo => operatorInfo.Title)
                .ToList();

            model.TotalResults = orderedMatches.Count;
            if (model.Page > model.TotalPages && model.TotalPages > 0) model.Page = model.TotalPages;
            model.Operators = orderedMatches
                .Skip((model.Page - 1) * model.PageSize)
                .Take(model.PageSize)
                .Select(operatorInfo => new CountryOperatorListItem
                {
                    ID = operatorInfo.ID,
                    Title = operatorInfo.Title,
                    CountryTitle = country.Title,
                    WebsiteURL = operatorInfo.WebsiteURL
                })
                .ToList();

            return View(model);
        }

        private void PopulateDuplicateWarnings(OperatorInfoManager manager, IEnumerable<Country> countries, CountryOperatorEditModel model)
        {
            var matches = manager.FindPotentialDuplicates(
                model.OperatorName,
                countries.FirstOrDefault(c => c.ID == model.CountryID)?.ISOCode,
                model.WebsiteURL,
                model.ContactEmail,
                model.ID > 1 ? model.ID : (int?)null);

            ViewBag.DuplicateTitleMatch = matches.FirstOrDefault(m => m.MatchType == OperatorMatchType.DuplicateTitle);
            ViewBag.PossibleDuplicates = matches.Where(m => m.RequiresConfirmation).ToList();
            ViewBag.OtherCountryMatches = matches.Where(m => m.MatchType == OperatorMatchType.OtherCountry).ToList();
        }

        [HttpGet]
        public ActionResult Add(int? countryId)
        {
            var user = GetCurrentUser();
            var countries = GetEditableCountries(user);
            if (countries.Count == 0) return Forbid();

            var model = new CountryOperatorEditModel { CountryID = countryId ?? countries[0].ID };
            PopulateCountries(countries, model.CountryID);
            return View("Edit", model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Add(CountryOperatorEditModel model)
        {
            model.ID = 0;
            return Save(model);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var user = GetCurrentUser();
            var countries = GetEditableCountries(user);
            if (countries.Count == 0) return Forbid();

            var operatorInfo = new OperatorInfoManager().GetOperatorInfo(id);
            var country = countries.FirstOrDefault(c => operatorInfo != null &&
                operatorInfo.Title.EndsWith(" (" + c.ISOCode + ")", StringComparison.OrdinalIgnoreCase));
            if (country == null) return Forbid();

            var model = new CountryOperatorEditModel
            {
                ID = operatorInfo.ID,
                CountryID = country.ID,
                OperatorName = operatorInfo.Title.Substring(0, operatorInfo.Title.Length - country.ISOCode.Length - 3),
                WebsiteURL = operatorInfo.WebsiteURL,
                Comments = operatorInfo.Comments,
                PhonePrimaryContact = operatorInfo.PhonePrimaryContact,
                PhoneSecondaryContact = operatorInfo.PhoneSecondaryContact,
                ContactEmail = operatorInfo.ContactEmail,
                FaultReportEmail = operatorInfo.FaultReportEmail
            };

            PopulateCountries(countries, model.CountryID);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CountryOperatorEditModel model)
        {
            if (id != model.ID) return BadRequest();
            return Save(model);
        }

        private ActionResult Save(CountryOperatorEditModel model)
        {
            var user = GetCurrentUser();
            var countries = GetEditableCountries(user);
            if (countries.Count == 0 || !CanEditCountry(user, model.CountryID)) return Forbid();

            PopulateCountries(countries, model.CountryID);
            PopulateDuplicateWarnings(new OperatorInfoManager(), countries, model);
            if (!ModelState.IsValid) return View("Edit", model);

            if (model.ID > 1)
            {
                var existing = new OperatorInfoManager().GetOperatorInfo(model.ID);
                if (existing == null) return NotFound();
                var suffix = " (" + countries.First(c => c.ID == model.CountryID).ISOCode + ")";
                if (!existing.Title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return Forbid();
            }

            try
            {
                var manager = new OperatorInfoManager();
                var saved = manager.SaveCountryOperator((int)UserID, model.CountryID, new OperatorInfo
                {
                    ID = model.ID,
                    Title = model.OperatorName,
                    WebsiteURL = model.WebsiteURL,
                    Comments = model.Comments,
                    PhonePrimaryContact = model.PhonePrimaryContact,
                    PhoneSecondaryContact = model.PhoneSecondaryContact,
                    ContactEmail = model.ContactEmail,
                    FaultReportEmail = model.FaultReportEmail
                }, model.ConfirmNotDuplicate);

                TempData["StatusMessage"] = model.ID > 1
                    ? $"Updated operator {saved.Title}."
                    : $"Created operator {saved.Title}.";
                return RedirectToAction(nameof(Edit), new { id = saved.ID, countryId = model.CountryID });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Edit", model);
            }
        }
    }
}
