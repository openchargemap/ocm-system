using System;
using System.Collections.Generic;

namespace OCM.API.Common.Model
{
    /// <summary>
    /// Types of permission level which can be assigned
    /// </summary>
    public enum PermissionLevel
    {
        Reader = 1,
        Editor = 100,
        Admin = 1000
    }

    //Optional filter to further refine which POIs user has permission to edit
    public class PermissionFilter
    {
        public int? OperatorID { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public double? DistanceKM { get; set; }
    }

    //Distinct permission assigned to a user
    public class UserPermission
    {
        public int? CountryID { get; set; }

        public PermissionLevel Level { get; set; }

        public PermissionFilter Filter { get; set; }
    }

    public class UserPermissionsContainer
    {
        public List<UserPermission> Permissions { get; set; }

        public string LegacyPermissions { get; set; }

        /// <summary>
        /// If true the user is blocked from contributing edits, comments and media. Stored as part of the existing user permissions metadata so no schema change is required.
        /// </summary>
        public bool? IsEditingBlocked { get; set; }

        /// <summary>
        /// Optional administrator note recording why editing was blocked for this user
        /// </summary>
        public string EditingBlockedReason { get; set; }

        /// <summary>
        /// Date editing was last blocked for this user
        /// </summary>
        public DateTime? DateEditingBlocked { get; set; }
    }
}