namespace OCM.API.Common.Model.Extensions
{
    public class UsageType : OCM.API.Common.Model.UsageType
    {
        public static Model.UsageType FromDataModel(Core.Data.UsageType source)
        {
            if (source == null) return null;

            return new Model.UsageType
            {
                ID = source.Id,
                Title = source.Title,
                IsAccessKeyRequired = source.IsAccessKeyRequired,
                IsMembershipRequired = source.IsMembershipRequired,
                IsPayAtLocation = source.IsPayAtLocation
            };

        }
    }
}