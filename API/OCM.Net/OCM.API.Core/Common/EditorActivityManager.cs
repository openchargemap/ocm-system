using Microsoft.EntityFrameworkCore;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.API.Common
{
    /// <summary>
    /// Reports on users holding editor permissions and how recently they have contributed, so inactive editors can be identified
    /// </summary>
    public class EditorActivityManager : ManagerBase
    {
        /// <summary>
        /// Editors who have contributed within this many days are considered active
        /// </summary>
        public const int RecentActivityDays = 90;

        /// <summary>
        /// Period over which contributions are counted, editors with no contributions in this period are considered inactive
        /// </summary>
        public const int ActivityPeriodMonths = 12;

        private class ActivityTotal
        {
            public int UserId { get; set; }
            public DateTime? DateLast { get; set; }
            public int CountInPeriod { get; set; }
        }

        /// <summary>
        /// Countries the user holds editor permission for, a null entry means the permission applies to all countries
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static List<int?> GetEditorCountryIDs(User user)
        {
            var permissions = UserManager.GetUserPermissions(user).Permissions;
            if (permissions == null) return new List<int?>();

            return permissions
                .Where(p => p.Level == PermissionLevel.Editor)
                .Select(p => p.CountryID)
                .Distinct()
                .ToList();
        }

        public static EditorActivityStatus GetActivityStatus(DateTime? dateLastActivity, DateTime asOfUtc)
        {
            if (dateLastActivity >= asOfUtc.AddDays(-RecentActivityDays)) return EditorActivityStatus.Active;
            if (dateLastActivity >= asOfUtc.AddMonths(-ActivityPeriodMonths)) return EditorActivityStatus.Occasional;
            return EditorActivityStatus.Inactive;
        }

        /// <summary>
        /// List editors under each country they can edit, editors of all countries are listed first under their own group
        /// </summary>
        /// <param name="editors"></param>
        /// <param name="countries">reference data used for country titles</param>
        /// <returns></returns>
        public static List<CountryEditorGroup> GroupEditorsByCountry(IEnumerable<EditorActivitySummary> editors, IEnumerable<Country> countries)
        {
            var countryLookup = countries.ToDictionary(c => c.ID);
            var groups = new List<CountryEditorGroup>();

            var allCountriesEditors = editors.Where(e => e.IsAllCountriesEditor).ToList();
            if (allCountriesEditors.Any())
            {
                groups.Add(new CountryEditorGroup { CountryTitle = "All Countries", Editors = SortByActivity(allCountriesEditors) });
            }

            groups.AddRange(editors
                .SelectMany(e => e.CountryIDs.Select(countryId => new { CountryID = countryId, Editor = e }))
                .GroupBy(x => x.CountryID)
                .Select(g =>
                {
                    countryLookup.TryGetValue(g.Key, out var country);
                    return new CountryEditorGroup
                    {
                        CountryID = g.Key,
                        CountryTitle = country?.Title ?? "Country " + g.Key,
                        CountryISOCode = country?.ISOCode,
                        Editors = SortByActivity(g.Select(x => x.Editor))
                    };
                })
                .OrderBy(g => g.CountryTitle));

            return groups;
        }

        private static List<EditorActivitySummary> SortByActivity(IEnumerable<EditorActivitySummary> editors)
        {
            return editors
                .OrderBy(e => e.ActivityStatus)
                .ThenByDescending(e => e.DateLastActivity)
                .ThenBy(e => e.Username)
                .ToList();
        }

        private static Dictionary<int, ActivityTotal> CombineTotals(params IEnumerable<ActivityTotal>[] sources)
        {
            return sources
                .SelectMany(s => s)
                .GroupBy(t => t.UserId)
                .ToDictionary(g => g.Key, g => new ActivityTotal
                {
                    UserId = g.Key,
                    DateLast = g.Max(t => t.DateLast),
                    CountInPeriod = g.Sum(t => t.CountInPeriod)
                });
        }

        public async Task<EditorActivityReport> GetEditorActivityReport(DateTime asOfUtc)
        {
            var report = new EditorActivityReport
            {
                DateGenerated = asOfUtc,
                ActivityPeriodStart = asOfUtc.AddMonths(-ActivityPeriodMonths),
                RecentActivityStart = asOfUtc.AddDays(-RecentActivityDays)
            };
            var periodStart = report.ActivityPeriodStart;

            //permissions are stored as JSON so editors are identified in memory, this only narrows the candidates to users with structured permissions
            var candidates = await dataModel.Users
                .AsNoTracking()
                .Where(u => u.Permissions != null && u.Permissions.Contains("Level"))
                .Select(u => new { u.Id, u.Username, u.EmailAddress, u.Permissions, u.DateCreated, u.DateLastLogin })
                .ToListAsync();

            foreach (var candidate in candidates)
            {
                var user = new User { ID = candidate.Id, Permissions = candidate.Permissions };
                var countryIds = GetEditorCountryIDs(user);
                if (!countryIds.Any()) continue;

                report.Editors.Add(new EditorActivitySummary
                {
                    UserID = candidate.Id,
                    Username = candidate.Username,
                    EmailAddress = candidate.EmailAddress,
                    DateCreated = candidate.DateCreated,
                    DateLastLogin = candidate.DateLastLogin,
                    IsAdministrator = UserManager.IsUserAdministrator(user),
                    IsEditingBlocked = UserManager.IsUserEditingBlocked(user),
                    IsAllCountriesEditor = countryIds.Contains(null),
                    CountryIDs = countryIds.Where(c => c.HasValue).Select(c => c.Value).ToList()
                });
            }

            if (!report.Editors.Any()) return report;

            var editorIds = report.Editors.Select(e => e.UserID).ToList();

            //edits submitted by the editor, including their own edits which are approved automatically
            var edits = await dataModel.EditQueueItems
                .Where(e => e.UserId != null && editorIds.Contains(e.UserId.Value))
                .GroupBy(e => e.UserId.Value)
                .Select(g => new ActivityTotal { UserId = g.Key, DateLast = g.Max(e => (DateTime?)e.DateSubmitted), CountInPeriod = g.Count(e => e.DateSubmitted >= periodStart) })
                .ToListAsync();

            //other users edits approved or rejected by the editor
            var editReviews = await dataModel.EditQueueItems
                .Where(e => e.IsProcessed && e.ProcessedByUserId != null && editorIds.Contains(e.ProcessedByUserId.Value) && e.UserId != e.ProcessedByUserId)
                .GroupBy(e => e.ProcessedByUserId.Value)
                .Select(g => new ActivityTotal { UserId = g.Key, DateLast = g.Max(e => e.DateProcessed), CountInPeriod = g.Count(e => e.DateProcessed >= periodStart) })
                .ToListAsync();

            var proposalReviews = await dataModel.OperatorProposals
                .Where(p => p.ReviewedByUserId != null && editorIds.Contains(p.ReviewedByUserId.Value))
                .GroupBy(p => p.ReviewedByUserId.Value)
                .Select(g => new ActivityTotal { UserId = g.Key, DateLast = g.Max(p => p.DateReviewed), CountInPeriod = g.Count(p => p.DateReviewed >= periodStart) })
                .ToListAsync();

            var comments = await dataModel.UserComments
                .Where(c => c.UserId != null && editorIds.Contains(c.UserId.Value))
                .GroupBy(c => c.UserId.Value)
                .Select(g => new ActivityTotal { UserId = g.Key, DateLast = g.Max(c => (DateTime?)c.DateCreated), CountInPeriod = g.Count(c => c.DateCreated >= periodStart) })
                .ToListAsync();

            var media = await dataModel.MediaItems
                .Where(m => editorIds.Contains(m.UserId))
                .GroupBy(m => m.UserId)
                .Select(g => new ActivityTotal { UserId = g.Key, DateLast = g.Max(m => (DateTime?)m.DateCreated), CountInPeriod = g.Count(m => m.DateCreated >= periodStart) })
                .ToListAsync();

            var editTotals = CombineTotals(edits);
            var reviewTotals = CombineTotals(editReviews, proposalReviews);
            var commentTotals = CombineTotals(comments, media);

            foreach (var editor in report.Editors)
            {
                if (editTotals.TryGetValue(editor.UserID, out var edit))
                {
                    editor.DateLastEdit = edit.DateLast;
                    editor.EditsInPeriod = edit.CountInPeriod;
                }

                if (reviewTotals.TryGetValue(editor.UserID, out var review))
                {
                    editor.DateLastReview = review.DateLast;
                    editor.ReviewsInPeriod = review.CountInPeriod;
                }

                if (commentTotals.TryGetValue(editor.UserID, out var comment))
                {
                    editor.DateLastComment = comment.DateLast;
                    editor.CommentsInPeriod = comment.CountInPeriod;
                }

                editor.ActivityStatus = GetActivityStatus(editor.DateLastActivity, asOfUtc);
            }

            var countryIdList = report.Editors.SelectMany(e => e.CountryIDs).Distinct().ToList();
            var countries = await dataModel.Countries
                .AsNoTracking()
                .Where(c => countryIdList.Contains(c.Id))
                .ToListAsync();

            report.Countries = GroupEditorsByCountry(report.Editors, Model.Extensions.Country.FromDataModel(countries));

            return report;
        }
    }
}
