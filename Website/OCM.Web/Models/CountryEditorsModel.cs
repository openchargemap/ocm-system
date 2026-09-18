using System.Collections.Generic;
using System.Linq;
using OCM.API.Common.Model;

namespace OCM.Web.Models
{
    public class CountryEditorsModel
    {
        public EditorActivityReport Report { get; set; }

        /// <summary>
        /// The country the list is filtered by, or null when editors for every country are listed.
        /// </summary>
        public int? CountryID { get; set; }

        /// <summary>
        /// The activity status the list is filtered by, or null for any status.
        /// </summary>
        public EditorActivityStatus? Status { get; set; }

        public bool IsFiltered => CountryID != null || Status != null;

        /// <summary>
        /// Countries which have at least one editor, for the country filter.
        /// </summary>
        public IEnumerable<CountryEditorGroup> CountriesWithEditors => Report.Countries.Where(c => c.CountryID != null);

        /// <summary>
        /// Country groups matching the current filters. Editors of all countries are kept when filtering by country as they can also edit it.
        /// </summary>
        public List<CountryEditorGroup> FilteredCountries =>
            Report.Countries
                .Where(c => CountryID == null || c.CountryID == null || c.CountryID == CountryID)
                .Select(c => new CountryEditorGroup
                {
                    CountryID = c.CountryID,
                    CountryTitle = c.CountryTitle,
                    CountryISOCode = c.CountryISOCode,
                    Editors = c.Editors.Where(e => Status == null || e.ActivityStatus == Status).ToList()
                })
                .Where(c => c.Editors.Any())
                .ToList();

        public string GetCountryTitle(int countryId)
        {
            return Report.Countries.FirstOrDefault(c => c.CountryID == countryId)?.CountryTitle ?? "Country " + countryId;
        }
    }
}
