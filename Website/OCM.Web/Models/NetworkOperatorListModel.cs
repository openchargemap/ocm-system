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
        /// The country to preselect when adding, or null to let the user pick one.
        /// </summary>
        public int? AddForCountryID { get; set; }

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
        /// The operator website as a link target, or null when it is not a usable http(s) address.
        /// </summary>
        public string WebsiteLink { get; set; }
    }
}
