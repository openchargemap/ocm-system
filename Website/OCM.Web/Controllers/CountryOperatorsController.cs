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
            ViewBag.CountryList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(countries, "ID", "Title", selectedCountryId);
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
        public ActionResult Edit(int? id, int? countryId)
        {
            var user = GetCurrentUser();
            var countries = GetEditableCountries(user);
            if (countries.Count == 0) return Forbid();

            var model = new CountryOperatorEditModel { CountryID = countryId ?? countries[0].ID };
            if (id.HasValue)
            {
                var operatorInfo = new OperatorInfoManager().GetOperatorInfo(id.Value);
                var country = countries.FirstOrDefault(c => operatorInfo != null && operatorInfo.Title.EndsWith(" (" + c.ISOCode + ")", StringComparison.OrdinalIgnoreCase));
                if (country == null) return Forbid();

                model.ID = operatorInfo.ID;
                model.CountryID = country.ID;
                model.OperatorName = operatorInfo.Title.Substring(0, operatorInfo.Title.Length - country.ISOCode.Length - 3);
                model.WebsiteURL = operatorInfo.WebsiteURL;
                model.Comments = operatorInfo.Comments;
                model.PhonePrimaryContact = operatorInfo.PhonePrimaryContact;
                model.PhoneSecondaryContact = operatorInfo.PhoneSecondaryContact;
                model.ContactEmail = operatorInfo.ContactEmail;
                model.FaultReportEmail = operatorInfo.FaultReportEmail;
            }

            PopulateCountries(countries, model.CountryID);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(CountryOperatorEditModel model)
        {
            var user = GetCurrentUser();
            var countries = GetEditableCountries(user);
            if (countries.Count == 0 || !CanEditCountry(user, model.CountryID)) return Forbid();

            PopulateCountries(countries, model.CountryID);
            PopulateDuplicateWarnings(new OperatorInfoManager(), countries, model);
            if (!ModelState.IsValid) return View(model);

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
                return View(model);
            }
        }
    }
}
