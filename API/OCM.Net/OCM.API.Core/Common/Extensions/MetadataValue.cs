using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class MetadataValue
    {
        public static Model.MetadataValue FromDataModel(Core.Data.MetadataValue source)
        {
            if (source == null) return null;

            return new Model.MetadataValue
            {
                ID = source.Id,
                MetadataFieldID =source.MetadataFieldId,
                MetadataFieldOption = MetadataFieldOption.FromDataModel(source.MetadataFieldOption),
                ItemValue = source.ItemValue
            };
        }
    }
}