namespace OCM.API.Common.Model.Extensions
{
    public class MediaItem
    {
        public static Model.MediaItem FromDataModel(Core.Data.MediaItem source)
        {
            if (source == null) return null;

            var m = new Model.MediaItem()
            {
                ID = source.Id,
                ChargePointID = source.ChargePointId,
                ItemURL = source.ItemUrl,
                ItemThumbnailURL = source.ItemThumbnailUrl,
                Comment = source.Comment,
                IsEnabled = source.IsEnabled ?? true,
                IsVideo = source.IsVideo,
                IsFeaturedItem = source.IsFeaturedItem,
                IsExternalResource = source.IsExternalResource,
                MetadataValue = source.MetadataValue,
                DateCreated = source.DateCreated,
                User = User.BasicFromDataModel(source.User)
            };

            if (m.ItemURL != null && m.ItemThumbnailURL == null)
            {
                // no thumbnail yet, use full size
                m.ItemThumbnailURL = m.ItemURL;
            }

            return m;

        }
    }
}
