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

            var f = new Model.MetadataField
            {
                ID = source.ID,
                Title = source.Title,
                MetadataGroupID = source.MetadataGroupID,
                DataTypeID = source.DataTypeID
            };

            if (source.MetadataFieldOptions!=null)
            {
                
                foreach (var o in source.MetadataFieldOptions)
                {
                    if (f.MetadataFieldOptions==null) f.MetadataFieldOptions = new List<Model.MetadataFieldOption>();
                    f.MetadataFieldOptions.Add(Extensions.MetadataFieldOption.FromDataModel(o));
                }
            }
            return f;
        }
    }
}