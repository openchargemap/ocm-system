using Newtonsoft.Json;
using OCM.API.Common;
using OCM.API.Common.Model;
using System.Collections.Generic;
using Xunit;

namespace OCM.API.Tests
{
    /// <summary>
    /// Unit tests for blocking specific users from making edits, the block is held in the user permissions metadata
    /// </summary>
    public class UserEditingBlockTests
    {
        private static string SerializePermissions(UserPermissionsContainer permissions)
        {
            return JsonConvert.SerializeObject(permissions, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private static ChargePoint GetTestPOI(int countryId = 1)
        {
            return new ChargePoint
            {
                ID = 1,
                AddressInfo = new AddressInfo { Title = "Test Location", CountryID = countryId, Country = new Country { ID = countryId } }
            };
        }

        [Fact]
        public void UserWithNoPermissionsIsNotBlocked()
        {
            Assert.False(UserManager.IsUserEditingBlocked(null));
            Assert.False(UserManager.IsUserEditingBlocked(new User { ID = 1 }));
            Assert.False(UserManager.IsUserEditingBlocked(new User { ID = 1, Permissions = "" }));
        }

        [Fact]
        public void LegacyPermissionsFormatDoesNotThrow()
        {
            //older accounts store permissions in a semicolon separated format which is not valid JSON
            var user = new User { ID = 1, Permissions = "[CountryLevel_Editor=All];[Administrator=true];" };

            Assert.False(UserManager.IsUserEditingBlocked(user));
            Assert.Equal("[CountryLevel_Editor=All];[Administrator=true];", UserManager.GetUserPermissions(user).LegacyPermissions);
        }

        [Fact]
        public void BlockedUserIsDetectedFromPermissionsMetadata()
        {
            var user = new User
            {
                ID = 1,
                Permissions = SerializePermissions(new UserPermissionsContainer { IsEditingBlocked = true, EditingBlockedReason = "Repeated inaccurate submissions" })
            };

            Assert.True(UserManager.IsUserEditingBlocked(user));
            Assert.Equal("Repeated inaccurate submissions", UserManager.GetEditingBlockedReason(user));
        }

        [Fact]
        public void BlockedUserReasonIsNotReportedOnceReinstated()
        {
            var user = new User
            {
                ID = 1,
                Permissions = SerializePermissions(new UserPermissionsContainer { EditingBlockedReason = "Previously blocked" })
            };

            Assert.False(UserManager.IsUserEditingBlocked(user));
            Assert.Null(UserManager.GetEditingBlockedReason(user));
        }

        [Fact]
        public void BlockDoesNotRemoveExistingEditorPermissions()
        {
            var permissions = new UserPermissionsContainer
            {
                Permissions = new List<UserPermission> { new UserPermission { CountryID = 1, Level = PermissionLevel.Editor } },
                IsEditingBlocked = true
            };

            var user = new User { ID = 1, Permissions = SerializePermissions(permissions) };

            //underlying permission is retained so it can be restored when the block is lifted
            Assert.True(UserManager.HasUserPermission(user, 1, PermissionLevel.Editor));
            Assert.True(UserManager.IsUserEditingBlocked(user));
        }

        [Fact]
        public void BlockedCountryEditorCannotEditPOI()
        {
            var permissions = new UserPermissionsContainer
            {
                Permissions = new List<UserPermission> { new UserPermission { CountryID = 1, Level = PermissionLevel.Editor } }
            };

            var user = new User { ID = 1, Permissions = SerializePermissions(permissions) };
            Assert.True(POIManager.CanUserEditPOI(GetTestPOI(), user));

            permissions.IsEditingBlocked = true;
            user.Permissions = SerializePermissions(permissions);
            Assert.False(POIManager.CanUserEditPOI(GetTestPOI(), user));
        }

        [Fact]
        public void BlockedAdministratorCannotEditPOI()
        {
            var permissions = new UserPermissionsContainer
            {
                Permissions = new List<UserPermission> { new UserPermission { Level = PermissionLevel.Admin } },
                IsEditingBlocked = true
            };

            var user = new User { ID = 1, Permissions = SerializePermissions(permissions) };

            Assert.True(UserManager.IsUserAdministrator(user));
            Assert.False(POIManager.CanUserEditPOI(GetTestPOI(), user));
        }

        [Fact]
        public void BlockedMessageDirectsUserToCommunityForum()
        {
            Assert.Contains("https://community.openchargemap.org/", UserManager.EditingBlockedMessage);
        }
    }
}
