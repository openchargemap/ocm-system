using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class MetadataField
    {
        public static Model.MetadataField FromDataModel(Core.Data.MetadataField source)
        {
            if (source == null) return null;

            return new Model.MetadataField
            {
                ID = source.ID,
                Title = source.Title,
                MetadataGroupID = source.MetadataGroupID,
                DataTypeID = source.DataTypeID
                //DataType = DataType.FromDataModel(source.DataType)
            };
        }
    }
}