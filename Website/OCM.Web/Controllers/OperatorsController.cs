using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OCM.API.Common;
using OCM.API.Common.Model;
using OCM.API.Utils;

namespace OCM.MVC.Controllers
{
    [AllowAnonymous]
    [Route("operators")]
    public class OperatorsController : BaseController
    {
        private const int PageSize = 25;

        [HttpGet("")]
        public ActionResult Index(string keyword, int? countryId, int pageIndex = 1)
        {
            keyword = keyword?.Trim();
            ViewData["keyword"] = keyword;
            ViewData["countryId"] = countryId;

            var countries = new ReferenceDataManager().GetCountries(false);
            var countryOptions = countries
                .OrderBy(country => country.Title)
                .Select(country => new SelectListItem
                {
                    Value = country.ID.ToString(),
                    Text = country.Title,
                    Selected = countryId == country.ID
                })
                .ToList();
            countryOptions.Insert(0, new SelectListItem { Value = "", Text = "All countries and global operators", Selected = !countryId.HasValue });
            countryOptions.Insert(1, new SelectListItem { Value = "-1", Text = "Global / multinational", Selected = countryId == -1 });
            ViewBag.CountryList = countryOptions;

            if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length < 2)
            {
                ViewBag.SearchTooShort = true;
                return View(new PaginatedCollection<OperatorInfo>(new List<OperatorInfo>(), 0, 1, PageSize));
            }

            var operators = new OperatorInfoManager().GetOperators().Where(operatorInfo => operatorInfo.ID > 1);
            if (!string.IsNullOrWhiteSpace(keyword))
                operators = operators.Where(operatorInfo => operatorInfo.Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            if (countryId == -1)
            {
                operators = operators.Where(operatorInfo => !countries.Any(country =>
                    operatorInfo.Title.EndsWith(" (" + country.ISOCode + ")", StringComparison.OrdinalIgnoreCase)));
            }
            else if (countryId.HasValue)
            {
                var country = countries.FirstOrDefault(item => item.ID == countryId.Value);
                if (country == null) return BadRequest();
                var suffix = " (" + country.ISOCode + ")";
                operators = operators.Where(operatorInfo => operatorInfo.Title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            }

            var matches = operators.OrderBy(operatorInfo => operatorInfo.Title).ToList();
            var totalPages = (int)Math.Ceiling(matches.Count / (double)PageSize);
            pageIndex = Math.Max(1, pageIndex);
            if (totalPages > 0) pageIndex = Math.Min(pageIndex, totalPages);

            return View(new PaginatedCollection<OperatorInfo>(
                matches.Skip((pageIndex - 1) * PageSize).Take(PageSize).ToList(),
                matches.Count,
                pageIndex,
                PageSize));
        }

        [HttpGet("{id:int}")]
        public ActionResult Details(int id)
        {
            var operatorInfo = new OperatorInfoManager().GetOperatorInfo(id);
            if (operatorInfo == null || operatorInfo.ID <= 1) return NotFound();

            var countryCode = OperatorInfoManager.GetCountryCodeFromTitle(operatorInfo.Title);
            var country = string.IsNullOrWhiteSpace(countryCode)
                ? null
                : new ReferenceDataManager().GetCountries(false)
                    .FirstOrDefault(item => string.Equals(item.ISOCode, countryCode, StringComparison.OrdinalIgnoreCase));
            ViewBag.CountryTitle = country?.Title ?? "Global / multinational";
            return View(operatorInfo);
        }
    }
}
