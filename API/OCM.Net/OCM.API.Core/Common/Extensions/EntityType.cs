namespace OCM.API.Common.Model.Extensions
{
    public class EntityType
    {
        public static Model.EntityType FromDataModel(Core.Data.EntityType source)
        {
            if (source == null) return null;

            return new Model.EntityType
            {
                ID = source.Id,
                Title = source.Title
            };
        }
    }
}