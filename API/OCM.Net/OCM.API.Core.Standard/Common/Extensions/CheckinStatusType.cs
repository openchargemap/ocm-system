using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class CheckinStatusType
    {
        public static Model.CheckinStatusType FromDataModel(Core.Data.CheckinStatusType source)
        {
            if (source == null) return null;

            return new Model.CheckinStatusType
            {
                ID = source.Id,
                Title = source.Title,
                IsPositive = source.IsPositive,
                IsAutomatedCheckin = source.IsAutomatedCheckin
            };
        }
    }
}