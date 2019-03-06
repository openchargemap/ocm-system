using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class User
    {
        public static string GetProfileImageURL(Core.Data.User source)
        {
            if (String.IsNullOrEmpty(source.EmailAddress)) return null;
            var emailHash = OCM.Core.Util.SecurityHelper.GetMd5Hash(source.EmailAddress);
            return "https://www.gravatar.com/avatar/" + emailHash + "?s=80&d=robohash";
        }

        /// <summary>
        /// returns a User object with sensitive information removed
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Model.User BasicFromDataModel(Core.Data.User source)
        {
            if (source == null) return null;

            return new Model.User()
            {
                ID = source.ID,
                Username = source.Username,
                ReputationPoints = source.ReputationPoints,
                ProfileImageURL = GetProfileImageURL(source)
            };
        }

        public static Model.User PublicProfileFromDataModel(Core.Data.User source, bool includeEmailHash = false)
        {
            if (source == null) return null;

            return new Model.User()
            {
                ID = source.ID,
                Username = source.Username,
                ReputationPoints = source.ReputationPoints,
                Location = source.Location,
                Profile = source.Profile,
                DateCreated = source.DateCreated,
                DateLastLogin = source.DateLastLogin,
                ProfileImageURL = GetProfileImageURL(source)
            };
        }

        public static Model.User FromDataModel(Core.Data.User source)
        {
            if (source == null) return null;

            return new Model.User()
            {
                ID = source.ID,
                IdentityProvider = source.IdentityProvider,
                Identifier = source.Identifier,
                CurrentSessionToken = source.CurrentSessionToken,
                Username = source.Username,
                Profile = source.Profile,
                Location = source.Location,
                WebsiteURL = source.WebsiteURL,
                ReputationPoints = source.ReputationPoints,
                Permissions = source.Permissions,
                PermissionsRequested = source.PermissionsRequested,
                DateCreated = source.DateCreated,
                DateLastLogin = source.DateLastLogin,
                IsProfilePublic = source.IsProfilePublic,
                IsPublicChargingProvider = source.IsPublicChargingProvider,
                IsEmergencyChargingProvider = source.IsEmergencyChargingProvider,
                Latitude = source.Latitude,
                Longitude = source.Longitude,
                EmailAddress = source.EmailAddress,
                ProfileImageURL = GetProfileImageURL(source),
                SyncedSettings = source.SyncedSettings
            };
        }
    }
}