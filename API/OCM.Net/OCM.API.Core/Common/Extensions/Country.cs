using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.API.Common.Model.Extensions
{
    public class Country
    {
        public static Model.Country FromDataModel(Core.Data.Country source)
        {
            if (source == null) return null;

            return new Model.Country() { ID = source.ID, Title = source.Title, ISOCode = source.ISOCode };
        }
    }
}