using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class DataProvider
    {
        public static Model.DataProvider FromDataModel(Core.Data.DataProvider source)
        {
            if (source == null) return null;

            return new Model.DataProvider
            {
                ID = source.ID,
                Title = source.Title,
                WebsiteURL = source.WebsiteURL,
                Comments = source.Comments,
                DataProviderStatusType = DataProviderStatusType.FromDataModel(source.DataProviderStatusType)
            };
        }
    }
}