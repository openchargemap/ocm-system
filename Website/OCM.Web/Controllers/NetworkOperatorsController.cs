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
    public class NetworkOperatorsController : BaseController
    {
        /// <summary>
        /// The country the list is filtered by when it is first opened.
        /// </summary>
        private const string DefaultCountryISOCode = "US";

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

        private static string NormalizeISOCode(string isoCode)
        {
            return (isoCode ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string GetWebsiteLink(string websiteUrl)
        {
            return Uri.TryCreate(websiteUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? uri.AbsoluteUri
                : null;
        }

        private void PopulateCountries(IEnumerable<Country> countries, int selectedCountryId)
        {
            ViewBag.CountryList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(countries, "ID", "Title", selectedCountryId);
        }

        private void PopulateCountryFilter(IEnumerable<Country> countries, int selectedCountryId)
        {
            var options = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "0", Text = "All countries", Selected = selectedCountryId == 0 }
            };

            options.AddRange(countries.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.ID.ToString(),
                Text = c.Title,
                Selected = c.ID == selectedCountryId
            }));

            ViewBag.CountryFilterList = options;
        }

        private void PopulateDuplicateWarnings(OperatorInfoManager manager, IEnumerable<Country> countries, NetworkOperatorAddModel model)
        {
            var matches = manager.FindPotentialDuplicates(
                model.OperatorName,
                countries.FirstOrDefault(c => c.ID == model.CountryID)?.ISOCode,
                model.WebsiteURL,
                model.ContactEmail);

            ViewBag.DuplicateTitleMatch = matches.FirstOrDefault(m => m.MatchType == OperatorMatchType.DuplicateTitle);
            ViewBag.PossibleDuplicates = matches.Where(m => m.RequiresConfirmation).ToList();
            ViewBag.OtherCountryMatches = matches.Where(m => m.MatchType == OperatorMatchType.OtherCountry).ToList();
        }

        /// <summary>
        /// Lists operators, filtered to one country. Countries are matched on the code in the operator title, so
        /// operators which are not country specific only appear when no country is selected.
        /// </summary>
        /// <param name="countryId">The country to list. Omitted on the first visit, which defaults to the United States, or zero to list every country.</param>
        [HttpGet]
        public ActionResult Index(int? countryId)
        {
            var user = GetCurrentUser();
            var allCountries = new ReferenceDataManager().GetCountries(false);
            var editableISOCodes = new HashSet<string>(GetEditableCountries(user).Select(c => NormalizeISOCode(c.ISOCode)), StringComparer.Ordinal);

            var selectedCountry = countryId.HasValue
                ? allCountries.FirstOrDefault(c => c.ID == countryId.Value)
                : allCountries.FirstOrDefault(c => NormalizeISOCode(c.ISOCode) == DefaultCountryISOCode);
            var selectedISOCode = selectedCountry != null ? NormalizeISOCode(selectedCountry.ISOCode) : null;

            var operators = new OperatorInfoManager().GetOperators()
                .Select(o => new NetworkOperatorListItem
                {
                    Operator = o,
                    CountryCode = OperatorInfoManager.GetCountryCodeFromTitle(o.Title),
                    WebsiteLink = GetWebsiteLink(o.WebsiteURL)
                })
                .Where(o => selectedISOCode == null || o.CountryCode == selectedISOCode)
                .ToList();

            PopulateCountryFilter(allCountries, selectedCountry?.ID ?? 0);

            return View(new NetworkOperatorListModel
            {
                Country = selectedCountry,
                CanAddOperator = editableISOCodes.Count > 0,
                AddForCountryID = selectedISOCode != null && editableISOCodes.Contains(selectedISOCode) ? selectedCountry.ID : (int?)null,
                Operators = operators
            });
        }

        [HttpGet]
        public ActionResult Add(int? countryId)
        {
            var user = GetCurrentUser();
            var countries = GetEditableCountries(user);
            if (countries.Count == 0) return Forbid();

            var model = new NetworkOperatorAddModel { CountryID = countryId ?? countries[0].ID };

            PopulateCountries(countries, model.CountryID);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Add(NetworkOperatorAddModel model)
        {
            var user = GetCurrentUser();
            var countries = GetEditableCountries(user);
            if (countries.Count == 0 || !CanEditCountry(user, model.CountryID)) return Forbid();

            PopulateCountries(countries, model.CountryID);
            PopulateDuplicateWarnings(new OperatorInfoManager(), countries, model);
            if (!ModelState.IsValid) return View(model);

            try
            {
                var added = new OperatorInfoManager().AddCountryOperator((int)UserID, model.CountryID, new OperatorInfo
                {
                    Title = model.OperatorName,
                    WebsiteURL = model.WebsiteURL,
                    Comments = model.Comments,
                    PhonePrimaryContact = model.PhonePrimaryContact,
                    PhoneSecondaryContact = model.PhoneSecondaryContact,
                    ContactEmail = model.ContactEmail,
                    FaultReportEmail = model.FaultReportEmail
                }, model.ConfirmNotDuplicate);

                TempData["StatusMessage"] = $"Added operator {added.Title}.";
                return RedirectToAction(nameof(Index), new { countryId = model.CountryID });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }
    }
}
