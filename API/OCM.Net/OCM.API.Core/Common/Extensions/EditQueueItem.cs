namespace OCM.API.Common.Model.Extensions
{
    public class EditQueueItem
    {
        public static Model.EditQueueItem FromDataModel(Core.Data.EditQueueItem source)
        {
            return new Model.EditQueueItem
            {
                ID = source.Id,
                User = User.BasicFromDataModel(source.User),
                Comment = source.Comment,
                IsProcessed = source.IsProcessed,
                ProcessedByUser = User.BasicFromDataModel(source.ProcessedByUser),
                DateSubmitted = source.DateSubmitted,
                DateProcessed = source.DateProcessed,
                EditData = source.EditData,
                PreviousData = source.PreviousData,
                EntityID = source.EntityId,
                EntityType = EntityType.FromDataModel(source.EntityType)
            };
        }
    }
}