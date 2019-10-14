using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class CurrentType
    {
        public static Model.CurrentType FromDataModel(Core.Data.CurrentType source)
        {
            if (source == null) return null;

            return new Model.CurrentType
            {
                ID = source.Id,
                Title = source.Title,
                Description = source.Description
            };
        }
    }
}