using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class RegisteredApplication
    {
        public static Model.RegisteredApplication FromDataModel(Core.Data.RegisteredApplication source, bool includeSensitiveInfo = false)
        {
            if (source == null) return null;

            var a= new Model.RegisteredApplication
            {
                ID = source.Id,
                Title = source.Title,
                Description = source.Description,
                WebsiteURL = source.WebsiteUrl,
                IsEnabled = source.IsEnabled,
                IsWriteEnabled = source.IsWriteEnabled,
                DateCreated = source.DateCreated
            };

            if (includeSensitiveInfo)
            {
                a.AppID = source.AppId;
                a.DateAPIKeyLastUsed = source.DateApikeyLastUsed;
                //DateAPIKeyUpdated  = source.DateAPIKeyUpdated
                a.PrimaryAPIKey = source.PrimaryApikey;
                a.DeprecatedAPIKey = source.DeprecatedApikey;
                a.UserID = source.UserId;
            }

            return a;
        }

        public static List<Model.RegisteredApplication> FromDataModel(IEnumerable<Core.Data.RegisteredApplication> source, bool includeSensitiveInfo = false)
        {
            if (source == null) return null;
            var list = new List<Model.RegisteredApplication>();
            foreach (var o in source)
            {
                list.Add(FromDataModel(o, includeSensitiveInfo));
            }

            return list;
        }
    }
}