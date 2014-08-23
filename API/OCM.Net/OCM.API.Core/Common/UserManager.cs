using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common
{
    public enum StandardPermissionAttributes
    {
        Administrator = 1,
        CountryLevel_Editor = 100,
        CountryLevel_Approver = 200
    }

    public class UserManager
    {
        private OCM.Core.Data.OCMEntities dataModel = new Core.Data.OCMEntities();

        public User GetUser(int id)
        {
            var userDetails = dataModel.Users.FirstOrDefault(u => u.ID == id);
            if (userDetails != null)
            {
                return Model.Extensions.User.FromDataModel(userDetails);
            }
            else
            {
                return null;
            }
        }

        public User GetUserFromIdentifier(string Identifier, string SessionToken)
        {
            if (Identifier == null || SessionToken == null) return null;

            var userDetails = dataModel.Users.FirstOrDefault(u => u.Identifier == Identifier);
            if (userDetails != null)
            {
                User user = Model.Extensions.User.FromDataModel(userDetails);
                if (user.CurrentSessionToken != SessionToken)
                    user.IsCurrentSessionTokenValid = false;
                else
                    user.IsCurrentSessionTokenValid = true;

                return user;
            }
            else
            {
                return null;
            }
        }

        public void AddReputationPoints(int userId, int amount)
        {
            var user = Model.Extensions.User.FromDataModel(dataModel.Users.FirstOrDefault(u => u.ID == userId));
            this.AddReputationPoints(user, amount);
        }

        /// <summary>
        /// Add reputation points to a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="amount"></param>
        public void AddReputationPoints(User user, int amount)
        {
            try
            {
                if (user != null)
                {
                    if (user.ID > 0)
                    {
                        var userData = dataModel.Users.First(u => u.ID == user.ID);
                        if (userData != null)
                        {
                            if (userData.ReputationPoints == null) userData.ReputationPoints = 0;
                            userData.ReputationPoints += amount;
                            dataModel.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception)
            {
                //could not add points to given user
            }
        }

        public void AssignNewSessionToken(int userId)
        {
            var user = dataModel.Users.FirstOrDefault(u => u.ID == userId);
            if (user != null)
            {
                user.CurrentSessionToken = Guid.NewGuid().ToString();
                dataModel.SaveChanges();
            }
        }

        /// <summary>
        /// Apply updates to a user profile
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool UpdateUserProfile(User updatedProfile, bool allowAdminChanges)
        {
            try
            {
                if (updatedProfile != null)
                {
                    if (updatedProfile.ID > 0)
                    {
                        var userData = dataModel.Users.First(u => u.ID == updatedProfile.ID);
                        if (userData != null)
                        {
                            userData.Location = updatedProfile.Location;
                            userData.Profile = updatedProfile.Profile;
                            userData.Username = updatedProfile.Username;
                            userData.WebsiteURL = updatedProfile.WebsiteURL;

                            userData.IsProfilePublic = (updatedProfile.IsProfilePublic != null ? (bool)updatedProfile.IsProfilePublic : false);
                            userData.IsPublicChargingProvider = (updatedProfile.IsPublicChargingProvider != null ? (bool)updatedProfile.IsPublicChargingProvider : false);
                            userData.IsEmergencyChargingProvider = (updatedProfile.IsEmergencyChargingProvider != null ? (bool)updatedProfile.IsEmergencyChargingProvider : false);
                            userData.EmailAddress = updatedProfile.EmailAddress;
                            userData.Latitude = updatedProfile.Latitude;
                            userData.Longitude = updatedProfile.Longitude;

                            //TODO: SyncedSettings
                            if (allowAdminChanges)
                            {
                                userData.ReputationPoints = updatedProfile.ReputationPoints;
                                userData.Identifier = updatedProfile.Identifier;
                                userData.Permissions = updatedProfile.Permissions;
                                userData.IdentityProvider = updatedProfile.IdentityProvider;
                            }

                            dataModel.SaveChanges();

                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                //could not update user
                return false;
            }

            return false;
        }

        /// <summary>
        /// Returns true if users exists but the provided session token is invalid
        /// </summary>
        /// <param name="Identifier"></param>
        /// <param name="SessionToken"></param>
        /// <returns></returns>
        public bool HasUserSessionExpired(string Identifier, string SessionToken)
        {
            var userDetails = dataModel.Users.FirstOrDefault(u => u.Identifier == Identifier);
            if (userDetails != null)
            {
                if (userDetails.CurrentSessionToken == SessionToken) return false;
                else return true;
            }
            return false;
        }

        public List<User> GetUsers()
        {
            List<User> list = new List<User>();
            var userList = dataModel.Users.Where(u => u.Identifier != null).OrderBy(u => u.Username);
            foreach (var user in userList)
            {
                list.Add(Model.Extensions.User.FromDataModel(user));
            }
            return list;
        }

        public static bool HasUserPermission(User user, StandardPermissionAttributes permissionAttribute, string attributeValue)
        {
            if (user != null && attributeValue != null)
            {
                if (user.Permissions != null)
                {
                    if (user.Permissions.Contains("[" + permissionAttribute.ToString() + "=" + attributeValue + "]"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsUserAdministrator(User user)
        {
            return HasUserPermission(user, StandardPermissionAttributes.Administrator, "true");
        }

        public bool GrantPermission(User administrator, User user, StandardPermissionAttributes permissionAttribute, string attributeValue, bool removeOnly)
        {
            if (IsUserAdministrator(administrator))
            {
                string attributeTag = "[" + permissionAttribute.ToString() + "=" + attributeValue + "];";

                var userDetails = dataModel.Users.FirstOrDefault(u => u.ID == user.ID);
                if (userDetails != null)
                {
                    if (userDetails.Permissions == null) userDetails.Permissions = "";

                    //append permission attribute for user
                    //format is [AttributeName1=Value];[AttributeName2=Value];

                    if (!userDetails.Permissions.Contains(permissionAttribute.ToString()))
                    {
                        if (!userDetails.Permissions.EndsWith(";") && userDetails.Permissions != "") userDetails.Permissions += ";";
                        userDetails.Permissions += attributeTag;
                        AuditLogManager.Log(administrator, AuditEventType.PermissionGranted, "User: " + user.ID + "; Permission:" + permissionAttribute.ToString(), null);
                    }
                    else
                    {
                        if (removeOnly)
                        {
                            userDetails.Permissions.Replace(attributeTag, "");
                            AuditLogManager.Log(administrator, AuditEventType.PermissionRemoved, "User: " + user.ID + "; Permission:" + permissionAttribute.ToString(), null);
                        }
                    }

                    //remove requested permission attribute if it exists
                    if (userDetails.PermissionsRequested != null)
                    {
                        userDetails.PermissionsRequested.Replace(attributeTag, "");
                    }

                    dataModel.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}