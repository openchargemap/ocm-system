using System.Collections.Generic;

namespace OCM.API.Common.Model.Extensions
{
    public class Country
    {
        public static Model.Country FromDataModel(Core.Data.Country source)
        {
            if (source == null) return null;

            return new Model.Country() { ID = source.Id, Title = source.Title, ISOCode = source.Isocode, ContinentCode = source.ContinentCode };
        }

        public static List<Model.Country> FromDataModel(IEnumerable<Core.Data.Country> source)
        {
            if (source == null) return null;

            var list = new List<Model.Country>();
            foreach (var c in source)
            {
                list.Add(FromDataModel(c));
            }
            return list;
        }
    }
}