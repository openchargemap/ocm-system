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
        private const int PageSize = 25;

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

        private bool CanEditOperator(User user, OperatorInfo operatorInfo, IEnumerable<Country> countries)
        {
            if (user == null || operatorInfo == null || operatorInfo.ID <= 1) return false;
            if (UserManager.IsUserAdministrator(user)) return true;

            var countryCode = OperatorInfoManager.GetCountryCodeFromTitle(operatorInfo.Title);
            var country = countries.FirstOrDefault(c => string.Equals(
                NormalizeISOCode(c.ISOCode), NormalizeISOCode(countryCode), StringComparison.Ordinal));
            return country != null && CanEditCountry(user, country.ID);
        }

        private static string GetOperatorName(string title, string isoCode)
        {
            var suffix = " (" + NormalizeISOCode(isoCode) + ")";
            return title != null && title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? title.Substring(0, title.Length - suffix.Length).Trim()
                : title;
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

        private void PopulateDuplicateWarnings(OperatorInfoManager manager, IEnumerable<Country> countries, NetworkOperatorAddModel model, int? excludedOperatorId = null)
        {
            var matches = manager.FindPotentialDuplicates(
                model.OperatorName,
                countries.FirstOrDefault(c => c.ID == model.CountryID)?.ISOCode,
                model.WebsiteURL,
                model.ContactEmail,
                excludedOperatorId);

            ViewBag.DuplicateTitleMatch = matches.FirstOrDefault(m => m.MatchType == OperatorMatchType.DuplicateTitle);
            ViewBag.PossibleDuplicates = matches.Where(m => m.RequiresConfirmation).ToList();
            ViewBag.OtherCountryMatches = matches.Where(m => m.MatchType == OperatorMatchType.OtherCountry).ToList();
        }

        /// <summary>
        /// Lists operators, either filtered to one country or searched by name across every country. Countries are
        /// matched on the code in the operator title, so operators which are not country specific only appear when
        /// no country is selected, and sort last in a search.
        /// </summary>
        /// <param name="countryId">The country to list. Omitted on the first visit, which defaults to the United States, or zero to list every country. Ignored while searching.</param>
        /// <param name="search">An operator name to search for across all countries. Takes precedence over countryId.</param>
        [HttpGet]
        public ActionResult Index(int? countryId, string search, int page = 1)
        {
            var user = GetCurrentUser();
            var allCountries = new ReferenceDataManager().GetCountries(false);
            var editableISOCodes = new HashSet<string>(GetEditableCountries(user).Select(c => NormalizeISOCode(c.ISOCode)), StringComparer.Ordinal);
            var countryNamesByISOCode = allCountries
                .GroupBy(c => NormalizeISOCode(c.ISOCode))
                .ToDictionary(g => g.Key, g => g.First().Title, StringComparer.Ordinal);

            var selectedCountry = countryId.HasValue
                ? allCountries.FirstOrDefault(c => c.ID == countryId.Value)
                : allCountries.FirstOrDefault(c => NormalizeISOCode(c.ISOCode) == DefaultCountryISOCode);
            var selectedISOCode = selectedCountry != null ? NormalizeISOCode(selectedCountry.ISOCode) : null;

            // a name search covers every country, so the country filter only applies when nothing is being searched for
            var isSearch = !string.IsNullOrWhiteSpace(search);

            var operators = new OperatorInfoManager().GetOperators()
                .Where(o => isSearch
                    ? OperatorInfoManager.MatchesNameSearch(o.Title, search)
                    : selectedISOCode == null || OperatorInfoManager.GetCountryCodeFromTitle(o.Title) == selectedISOCode)
                .Select(o =>
                {
                    var countryCode = OperatorInfoManager.GetCountryCodeFromTitle(o.Title);
                    return new NetworkOperatorListItem
                    {
                        Operator = o,
                        CountryCode = countryCode,
                        CountryName = countryCode != null && countryNamesByISOCode.TryGetValue(countryCode, out var countryName) ? countryName : null,
                        WebsiteLink = GetWebsiteLink(o.WebsiteURL)
                    };
                })
                .OrderBy(o => o.CountryName == null)
                .ThenBy(o => o.CountryName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(o => o.Operator.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            page = Math.Max(1, page);
            var totalResults = operators.Count;
            var totalPages = (int)Math.Ceiling(totalResults / (double)PageSize);
            if (totalPages > 0) page = Math.Min(page, totalPages);
            var pagedOperators = operators
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();
            foreach (var item in pagedOperators)
                item.CanEdit = CanEditOperator(user, item.Operator, allCountries);

            PopulateCountryFilter(allCountries, selectedCountry?.ID ?? 0);

            return View(new NetworkOperatorListModel
            {
                Country = isSearch ? null : selectedCountry,
                SearchTerm = isSearch ? search.Trim() : null,
                CanAddOperator = editableISOCodes.Count > 0,
                AddForCountryID = selectedISOCode != null && editableISOCodes.Contains(selectedISOCode) ? selectedCountry.ID : (int?)null,
                Operators = pagedOperators,
                Page = page,
                PageSize = PageSize,
                TotalResults = totalResults
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

        [HttpGet("/NetworkOperators/Edit/{id:int}")]
        public ActionResult Edit(int id)
        {
            var user = GetCurrentUser();
            var allCountries = new ReferenceDataManager().GetCountries(false);
            var operatorInfo = new OperatorInfoManager().GetOperatorInfo(id);
            if (operatorInfo == null || operatorInfo.ID <= 1) return NotFound();
            if (!CanEditOperator(user, operatorInfo, allCountries)) return Forbid();

            var countryCode = OperatorInfoManager.GetCountryCodeFromTitle(operatorInfo.Title);
            var country = allCountries.FirstOrDefault(c => string.Equals(
                NormalizeISOCode(c.ISOCode), NormalizeISOCode(countryCode), StringComparison.Ordinal));
            var model = new NetworkOperatorAddModel
            {
                ID = operatorInfo.ID,
                CountryID = country?.ID ?? 0,
                OperatorName = GetOperatorName(operatorInfo.Title, countryCode),
                WebsiteURL = operatorInfo.WebsiteURL,
                Comments = operatorInfo.Comments,
                PhonePrimaryContact = operatorInfo.PhonePrimaryContact,
                PhoneSecondaryContact = operatorInfo.PhoneSecondaryContact,
                ContactEmail = operatorInfo.ContactEmail,
                FaultReportEmail = operatorInfo.FaultReportEmail
            };

            ViewBag.IsEdit = true;
            PopulateCountries(GetEditableCountries(user), model.CountryID);
            return View("Add", model);
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

        [HttpPost("/NetworkOperators/Edit/{id:int}"), ValidateAntiForgeryToken]
        public ActionResult Edit(int id, NetworkOperatorAddModel model)
        {
            var user = GetCurrentUser();
            var allCountries = new ReferenceDataManager().GetCountries(false);
            var operatorInfo = new OperatorInfoManager().GetOperatorInfo(id);
            if (operatorInfo == null || operatorInfo.ID <= 1) return NotFound();
            if (!CanEditOperator(user, operatorInfo, allCountries)) return Forbid();

            var countryCode = OperatorInfoManager.GetCountryCodeFromTitle(operatorInfo.Title);
            var country = allCountries.FirstOrDefault(c => string.Equals(
                NormalizeISOCode(c.ISOCode), NormalizeISOCode(countryCode), StringComparison.Ordinal));
            model.ID = id;
            model.CountryID = country?.ID ?? 0;
            ViewBag.IsEdit = true;
            PopulateCountries(GetEditableCountries(user), model.CountryID);
            PopulateDuplicateWarnings(new OperatorInfoManager(), allCountries, model, id);
            var duplicateTitleMatch = ViewBag.DuplicateTitleMatch as OperatorMatch;
            if (duplicateTitleMatch != null)
            {
                ModelState.AddModelError(string.Empty, $"\"{duplicateTitleMatch.Operator.Title}\" already exists, so the operator name cannot be used again for this country.");
            }
            if (!ModelState.IsValid) return View("Add", model);

            try
            {
                var title = country == null
                    ? model.OperatorName?.Trim()
                    : model.OperatorName?.Trim() + " (" + NormalizeISOCode(country.ISOCode) + ")";
                new OperatorInfoManager().UpdateOperatorInfo((int)UserID, new OperatorInfo
                {
                    ID = id,
                    Title = title,
                    WebsiteURL = model.WebsiteURL,
                    Comments = model.Comments,
                    PhonePrimaryContact = model.PhonePrimaryContact,
                    PhoneSecondaryContact = model.PhoneSecondaryContact,
                    ContactEmail = model.ContactEmail,
                    FaultReportEmail = model.FaultReportEmail
                });

                TempData["StatusMessage"] = $"Updated operator {title}.";
                return RedirectToAction(nameof(Index), new { countryId = model.CountryID == 0 ? (int?)null : model.CountryID });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Add", model);
            }
        }
    }
}
