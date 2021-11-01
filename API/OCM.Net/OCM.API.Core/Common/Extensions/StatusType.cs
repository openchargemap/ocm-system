namespace OCM.API.Common.Model.Extensions
{
    public class StatusType
    {
        public static Model.StatusType FromDataModel(Core.Data.StatusType source)
        {
            if (source == null) return null;

            return new Model.StatusType
            {
                ID = source.Id,
                Title = source.Title,
                IsOperational = source.IsOperational,
                IsUserSelectable = (bool)source.IsUserSelectable
            };
        }
    }
}