using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class MediaItem
    {
        public static Model.MediaItem FromDataModel(Core.Data.MediaItem source)
        {
            if (source == null) return null;

            return new Model.MediaItem()
            {
                ID = source.ID,
                ChargePointID = source.ChargePointID,
                ItemURL = source.ItemURL,
                ItemThumbnailURL = source.ItemThumbnailURL,
                Comment = source.Comment,
                IsEnabled =  source.IsEnabled,
                IsVideo = source.IsVideo,
                IsFeaturedItem = source.IsFeaturedItem,
                IsExternalResource = source.IsExternalResource,
                MetadataValue = source.MetadataValue,
                DateCreated = source.DateCreated,
                User = User.BasicFromDataModel(source.User)
            };

        }
    }
}