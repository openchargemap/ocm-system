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

            return new Model.MetadataGroup
            {
                ID = source.ID,
                Title = source.Title,
                GroupOwner = User.BasicFromDataModel(source.GroupOwner),
                IsRestrictedEdit = source.IsRestrictedEdit
            };
        }
    }
}