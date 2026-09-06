using System.Collections.Generic;
using OCM.API.Common.Model;

namespace OCM.Web.Models
{
    public class NetworkOperatorListModel
    {
        /// <summary>
        /// The country the list is filtered by, or null when operators for every country are listed.
        /// </summary>
        public Country Country { get; set; }

        /// <summary>
        /// True when the user is an editor for at least one country, so has somewhere to add an operator.
        /// </summary>
        public bool CanAddOperator { get; set; }

        /// <summary>
        /// The operator name being searched for, or null when the list is filtered by country instead.
        /// </summary>
        public string SearchTerm { get; set; }

        /// <summary>
        /// True when a name search is being shown, which covers every country rather than the selected one.
        /// </summary>
        public bool IsSearch => !string.IsNullOrWhiteSpace(SearchTerm);

        /// <summary>
        /// The country to preselect when adding, or null to let the user pick one.
        /// </summary>
        public int? AddForCountryID { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 25;

        public int TotalResults { get; set; }

        public int TotalPages => (int)System.Math.Ceiling(TotalResults / (double)PageSize);

        public bool HasPreviousPage => Page > 1;

        public bool HasNextPage => Page < TotalPages;

        public List<NetworkOperatorListItem> Operators { get; set; } = new List<NetworkOperatorListItem>();
    }

    public class NetworkOperatorListItem
    {
        public OperatorInfo Operator { get; set; }

        /// <summary>
        /// The ISO country code taken from the operator title, or null for operators which are not country specific.
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// The country name for CountryCode, or null when the operator is not country specific.
        /// </summary>
        public string CountryName { get; set; }

        /// <summary>
        /// The operator website as a link target, or null when it is not a usable http(s) address.
        /// </summary>
        public string WebsiteLink { get; set; }

        public bool CanEdit { get; set; }
    }
}
