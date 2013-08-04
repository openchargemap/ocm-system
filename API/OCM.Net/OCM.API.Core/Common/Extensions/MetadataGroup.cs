using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class MetadataGroup
    {
        public static Model.MetadataGroup FromDataModel(Core.Data.MetadataGroup source)
        {
            if (source == null) return null;

            var group= new Model.MetadataGroup
            {
                ID = source.ID,
                Title = source.Title,
                DataProviderID = source.DataProviderID,
                IsRestrictedEdit = source.IsRestrictedEdit,
                IsPublicInterest = source.IsPublicInterest,
                MetadataFields = new List<Model.MetadataField>()
            };

            if (source.MetadataFields!=null)
            {
                foreach (var m in source.MetadataFields)
                {
                    group.MetadataFields.Add(MetadataField.FromDataModel(m));
                }
            }

            return group;
        }
    }
}