namespace OCM.API.Common.Model.Extensions
{
    public class AuditLog
    {
        public static Model.AuditLog FromDataModel(Core.Data.AuditLog source)
        {
            if (source == null) return null;

            return new Model.AuditLog()
            {
                ID = source.Id,
                EventDate = source.EventDate,
                UserID = source.UserId,
                EventDescription = source.EventDescription,
                Comment = source.Comment
            };
        }
    }
}