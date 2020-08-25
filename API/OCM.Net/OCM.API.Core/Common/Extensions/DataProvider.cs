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
                ID = source.Id,
                Title = source.Title,
                WebsiteURL = source.WebsiteUrl,
                Comments = source.Comments,
                DataProviderStatusType = DataProviderStatusType.FromDataModel(source.DataProviderStatusType),
                IsRestrictedEdit = source.IsRestrictedEdit,
                IsOpenDataLicensed = source.IsOpenDataLicensed,
                IsApprovedImport = source.IsApprovedImport,
                License = source.License,
                DateLastImported = source.DateLastImported
            };
        }
    }
}