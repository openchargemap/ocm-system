using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class StatusType
    {
        public static Model.StatusType FromDataModel(Core.Data.StatusType source)
        {
            if (source == null) return null;

            return new Model.StatusType
            {
                ID = source.ID,
                Title = source.Title,
                IsOperational = source.IsOperational,
                IsUserSelectable = source.IsUserSelectable
            };
        }
    }
}