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
                ID = source.ID,
                ChargePointID = source.ChargePointID,
                MetadataFieldID =source.MetadataFieldID,
                MetadataField = MetadataField.FromDataModel(source.MetadataField),
                ItemValue = source.ItemValue
            };
        }
    }
}