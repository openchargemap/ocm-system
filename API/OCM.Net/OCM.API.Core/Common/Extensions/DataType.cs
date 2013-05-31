using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class DataType
    {
        public static Model.DataType FromDataModel(Core.Data.DataType source)
        {
            if (source == null) return null;

            return new Model.DataType
            {
                ID = source.ID,
                Title = source.Title
            };
        }
    }
}