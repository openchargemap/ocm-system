using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    /// <summary>
    /// How recently an editor has contributed, based on their edits, reviews, comments and photos
    /// </summary>
    public enum EditorActivityStatus
    {
        /// <summary>
        /// Contributed within the recent activity period (90 days)
        /// </summary>
        Active = 1,

        /// <summary>
        /// Contributed within the activity period (12 months) but not recently
        /// </summary>
        Occasional = 2,

        /// <summary>
        /// No contributions within the activity period
        /// </summary>
        Inactive = 3
    }

    /// <summary>
    /// Summary of a single editor's permissions, sign in and contribution activity
    /// </summary>
    public class EditorActivitySummary
    {
        public int UserID { get; set; }

        public string Username { get; set; }

        public string EmailAddress { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime? DateLastLogin { get; set; }

        public bool IsAdministrator { get; set; }

        public bool IsEditingBlocked { get; set; }

        /// <summary>
        /// True if the editor permission applies to all countries
        /// </summary>
        public bool IsAllCountriesEditor { get; set; }

        /// <summary>
        /// Specific countries the user has editor permission for
        /// </summary>
        public List<int> CountryIDs { get; set; } = new List<int>();

        /// <summary>
        /// POI edits submitted by the editor
        /// </summary>
        public DateTime? DateLastEdit { get; set; }

        public int EditsInPeriod { get; set; }

        /// <summary>
        /// Other users' edits and operator proposals approved or rejected by the editor
        /// </summary>
        public DateTime? DateLastReview { get; set; }

        public int ReviewsInPeriod { get; set; }

        /// <summary>
        /// Comments, check-ins and photos added by the editor
        /// </summary>
        public DateTime? DateLastComment { get; set; }

        public int CommentsInPeriod { get; set; }

        public DateTime? DateLastActivity
        {
            get
            {
                var dates = new[] { DateLastEdit, DateLastReview, DateLastComment }.Where(d => d.HasValue);
                return dates.Any() ? dates.Max() : null;
            }
        }

        public EditorActivityStatus ActivityStatus { get; set; }
    }

    /// <summary>
    /// Editors holding editor permission for a country (or for all countries)
    /// </summary>
    public class CountryEditorGroup
    {
        /// <summary>
        /// Null for editors of all countries
        /// </summary>
        public int? CountryID { get; set; }

        public string CountryTitle { get; set; }

        public string CountryISOCode { get; set; }

        public List<EditorActivitySummary> Editors { get; set; } = new List<EditorActivitySummary>();
    }

    public class EditorActivityReport
    {
        public DateTime DateGenerated { get; set; }

        /// <summary>
        /// Start of the period used for activity counts
        /// </summary>
        public DateTime ActivityPeriodStart { get; set; }

        /// <summary>
        /// Contributions on or after this date mark an editor as active
        /// </summary>
        public DateTime RecentActivityStart { get; set; }

        public List<EditorActivitySummary> Editors { get; set; } = new List<EditorActivitySummary>();

        public List<CountryEditorGroup> Countries { get; set; } = new List<CountryEditorGroup>();
    }
}
