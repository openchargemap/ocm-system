using System.Collections.Generic;

namespace OCM.API.Common.Model.Extensions
{
    public class RegisteredApplicationUser
    {
        public static Model.RegisteredApplicationUser FromDataModel(Core.Data.RegisteredApplicationUser source)
        {
            if (source == null) return null;

            return new Model.RegisteredApplicationUser
            {
                ID = source.Id,
                UserID = source.UserId,
                RegisteredApplicationID = source.RegisteredApplicationId,
                RegisteredApplication = RegisteredApplication.FromDataModel(source.RegisteredApplication, false),
                IsEnabled = source.IsEnabled,
                IsWriteEnabled = source.IsWriteEnabled,
                APIKey = source.Apikey,
                DateCreated = source.DateCreated,

            };
        }

        public static List<Model.RegisteredApplicationUser> FromDataModel(IEnumerable<Core.Data.RegisteredApplicationUser> source)
        {
            if (source == null) return null;
            var list = new List<Model.RegisteredApplicationUser>();
            foreach (var o in source)
            {
                list.Add(FromDataModel(o));
            }

            return list;
        }
    }
}