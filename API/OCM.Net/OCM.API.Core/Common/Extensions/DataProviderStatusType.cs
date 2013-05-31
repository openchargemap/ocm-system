using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class DataProviderStatusType
    {
        public static Model.DataProviderStatusType FromDataModel(Core.Data.DataProviderStatusType s)
        {
            if (s == null) return null;

            return new Model.DataProviderStatusType
            {
                 ID = s.ID,
                 Title = s.Title,
                 IsProviderEnabled = s.IsProviderEnabled
            };
        }
    }
}