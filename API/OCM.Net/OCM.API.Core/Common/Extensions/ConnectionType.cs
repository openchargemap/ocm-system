using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class ConnectionType
    {
        public static Model.ConnectionType FromDataModel(Core.Data.ConnectionType source)
        {
            if (source == null) return null;

            return new Model.ConnectionType
            {
                ID = source.Id,
                Title = source.Title,
                FormalName = source.FormalName,
                IsDiscontinued = source.IsDiscontinued,
                IsObsolete = source.IsObsolete
            };
        }
    }
}