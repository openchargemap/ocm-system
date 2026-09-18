using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OCM.API.Tests
{
    /// <summary>
    /// Unit tests for the country editor activity report
    /// </summary>
    public class EditorActivityTests
    {
        private static readonly DateTime AsOf = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);

        private static User GetUserWithPermissions(params UserPermission[] permissions)
        {
            var container = new UserPermissionsContainer { Permissions = permissions.ToList() };
            return new User { ID = 1, Permissions = JsonConvert.SerializeObject(container, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }) };
        }

        [Fact]
        public void EditorCountriesAreReadFromPermissions()
        {
            var user = GetUserWithPermissions(
                new UserPermission { CountryID = 1, Level = PermissionLevel.Editor },
                new UserPermission { CountryID = 2, Level = PermissionLevel.Editor },
                new UserPermission { CountryID = 2, Level = PermissionLevel.Editor });

            Assert.Equal(new int?[] { 1, 2 }, EditorActivityManager.GetEditorCountryIDs(user));
        }

        [Fact]
        public void AllCountriesEditorHasNullCountry()
        {
            var user = GetUserWithPermissions(new UserPermission { Level = PermissionLevel.Editor });

            Assert.Equal(new int?[] { null }, EditorActivityManager.GetEditorCountryIDs(user));
        }

        [Fact]
        public void AdministratorWithoutEditorPermissionIsNotListedAsEditor()
        {
            var user = GetUserWithPermissions(new UserPermission { Level = PermissionLevel.Admin });

            Assert.Empty(EditorActivityManager.GetEditorCountryIDs(user));
        }

        [Fact]
        public void UsersWithoutStructuredPermissionsAreNotEditors()
        {
            Assert.Empty(EditorActivityManager.GetEditorCountryIDs(new User { ID = 1 }));
            Assert.Empty(EditorActivityManager.GetEditorCountryIDs(new User { ID = 1, Permissions = "[CountryLevel_Editor=1];" }));
            Assert.Empty(EditorActivityManager.GetEditorCountryIDs(new User { ID = 1, Permissions = JsonConvert.SerializeObject(new UserPermissionsContainer { IsEditingBlocked = true }) }));
        }

        [Fact]
        public void RemovingEditorPermissionsKeepsOtherPermissions()
        {
            var permissions = new UserPermissionsContainer
            {
                LegacyPermissions = "[CountryLevel_Editor=1];[Administrator=true];[CountryLevel_Editor=All];[CountryLevel_Editor=22]",
                Permissions = new List<UserPermission>
                {
                    new UserPermission { CountryID = 1, Level = PermissionLevel.Editor },
                    new UserPermission { Level = PermissionLevel.Admin },
                    new UserPermission { Level = PermissionLevel.Editor },
                    new UserPermission { CountryID = 22, Level = PermissionLevel.Editor }
                },
                IsEditingBlocked = true
            };

            Assert.True(UserManager.RemoveEditorPermissions(permissions));

            Assert.Equal("[Administrator=true];", permissions.LegacyPermissions);
            Assert.Equal(PermissionLevel.Admin, Assert.Single(permissions.Permissions).Level);
            Assert.True(permissions.IsEditingBlocked);

            var user = new User { ID = 1, Permissions = JsonConvert.SerializeObject(permissions) };
            Assert.Empty(EditorActivityManager.GetEditorCountryIDs(user));
            Assert.True(UserManager.IsUserAdministrator(user));
        }

        [Fact]
        public void RemovingEditorPermissionsFromNonEditorChangesNothing()
        {
            var permissions = new UserPermissionsContainer
            {
                LegacyPermissions = "[Administrator=true];",
                Permissions = new List<UserPermission> { new UserPermission { Level = PermissionLevel.Admin } }
            };

            Assert.False(UserManager.RemoveEditorPermissions(permissions));
            Assert.False(UserManager.RemoveEditorPermissions(new UserPermissionsContainer()));
            Assert.Equal("[Administrator=true];", permissions.LegacyPermissions);
            Assert.Single(permissions.Permissions);
        }

        [Fact]
        public void LegacyOnlyEditorPermissionsAreRemoved()
        {
            //older accounts may only hold the semicolon separated format
            var permissions = UserManager.GetUserPermissions(new User { ID = 1, Permissions = "[CountryLevel_Editor=5];" });

            Assert.True(UserManager.RemoveEditorPermissions(permissions));
            Assert.Equal("", permissions.LegacyPermissions);
        }

        [Theory]
        [InlineData(0, EditorActivityStatus.Active)]
        [InlineData(90, EditorActivityStatus.Active)]
        [InlineData(91, EditorActivityStatus.Occasional)]
        [InlineData(365, EditorActivityStatus.Occasional)]
        [InlineData(367, EditorActivityStatus.Inactive)]
        [InlineData(2000, EditorActivityStatus.Inactive)]
        public void ActivityStatusReflectsLastContribution(int daysAgo, EditorActivityStatus expected)
        {
            Assert.Equal(expected, EditorActivityManager.GetActivityStatus(AsOf.AddDays(-daysAgo), AsOf));
        }

        [Fact]
        public void EditorWithNoContributionsIsInactive()
        {
            Assert.Equal(EditorActivityStatus.Inactive, EditorActivityManager.GetActivityStatus(null, AsOf));
        }

        [Fact]
        public void LastActivityIsMostRecentContribution()
        {
            var editor = new EditorActivitySummary
            {
                DateLastEdit = AsOf.AddDays(-30),
                DateLastReview = AsOf.AddDays(-5),
                DateLastComment = null
            };

            Assert.Equal(AsOf.AddDays(-5), editor.DateLastActivity);
            Assert.Null(new EditorActivitySummary().DateLastActivity);
        }

        [Fact]
        public void EditorsAreGroupedUnderEachCountryTheyEdit()
        {
            var countries = new List<Country>
            {
                new Country { ID = 1, Title = "United Kingdom", ISOCode = "GB" },
                new Country { ID = 2, Title = "France", ISOCode = "FR" }
            };

            var inactiveEditor = new EditorActivitySummary { UserID = 10, Username = "inactive", CountryIDs = new List<int> { 1 }, ActivityStatus = EditorActivityStatus.Inactive };
            var activeEditor = new EditorActivitySummary { UserID = 11, Username = "active", CountryIDs = new List<int> { 1, 2 }, ActivityStatus = EditorActivityStatus.Active, DateLastEdit = AsOf };
            var globalEditor = new EditorActivitySummary { UserID = 12, Username = "global", IsAllCountriesEditor = true, ActivityStatus = EditorActivityStatus.Occasional };
            var unknownCountryEditor = new EditorActivitySummary { UserID = 13, Username = "unknown", CountryIDs = new List<int> { 99 }, ActivityStatus = EditorActivityStatus.Active };

            var groups = EditorActivityManager.GroupEditorsByCountry(new[] { inactiveEditor, activeEditor, globalEditor, unknownCountryEditor }, countries);

            //all countries group first, then countries by title
            Assert.Equal(new int?[] { null, 99, 2, 1 }, groups.Select(g => g.CountryID));
            Assert.Equal("All Countries", groups[0].CountryTitle);
            Assert.Equal(new[] { 12 }, groups[0].Editors.Select(e => e.UserID));

            Assert.Equal("Country 99", groups[1].CountryTitle);

            var france = groups[2];
            Assert.Equal("FR", france.CountryISOCode);
            Assert.Equal(new[] { 11 }, france.Editors.Select(e => e.UserID));

            //active editors are listed before inactive ones
            var uk = groups[3];
            Assert.Equal(new[] { 11, 10 }, uk.Editors.Select(e => e.UserID));
        }

        [Fact]
        public void NoAllCountriesGroupWithoutGlobalEditors()
        {
            var editor = new EditorActivitySummary { UserID = 10, CountryIDs = new List<int> { 1 } };

            var groups = EditorActivityManager.GroupEditorsByCountry(new[] { editor }, new List<Country>());

            Assert.Single(groups);
            Assert.Equal(1, groups[0].CountryID);
        }
    }
}
