using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class DataProviderUser
    {
        public static Model.DataProviderUser FromDataModel(Core.Data.DataProviderUser source)
        {
            if (source == null) return null;

            return new Model.DataProviderUser
            {
                ID = source.ID,
                DataProvider = DataProvider.FromDataModel(source.DataProvider),
                User = User.BasicFromDataModel(source.User),
                IsDataProviderAdmin=source.IsDataProviderAdmin
            };
        }
    }
}