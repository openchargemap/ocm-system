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
    }
}