using System.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using ImageResizer;
using OCM.Core.Data;
using OCM.Core.Util;

namespace OCM.API.Common
{

    public class MediaItemManager
    {

        public MediaItem AddPOIMediaItem(string tempFolder, string sourceImageFile, int chargePointId, string comment, bool isVideo, int userId)
        {
            var dataModel = new OCMEntities();

            var mediaItem = new MediaItem();

            var poi = new POIManager().Get(chargePointId);
            string[] urls = UploadPOIImageToStorage(tempFolder, sourceImageFile, poi);
            mediaItem.ItemURL = urls[0];
            mediaItem.ItemThumbnailURL = urls[1];

            mediaItem.User = dataModel.Users.FirstOrDefault(u => u.ID == userId);
            mediaItem.ChargePoint = dataModel.ChargePoints.FirstOrDefault(cp => cp.ID == chargePointId);
            mediaItem.Comment = comment;
            mediaItem.DateCreated = DateTime.UtcNow;
            mediaItem.IsEnabled = true;
            mediaItem.IsExternalResource = false;
            mediaItem.IsVideo = isVideo;

            dataModel.MediaItems.Add(mediaItem);
            dataModel.SaveChanges();
            return mediaItem;
        }

        private void GenerateImageThumbnails(string sourceFile, string destFile, int maxWidth)
        {
            ImageBuilder.Current.Build(sourceFile, destFile, new ResizeSettings("width=" + maxWidth));
        }

        public string[] UploadPOIImageToStorage(string tempFolder, string sourceImageFile, Model.ChargePoint poi)
        {
            string extension = sourceImageFile.Substring(sourceImageFile.LastIndexOf('.'), sourceImageFile.Length - sourceImageFile.LastIndexOf('.')).ToLower();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif") return null;
            var storage = new StorageManager();
            try
            {
                //TODO: allocate sequences properly
                string destFolderPrefix = poi.AddressInfo.Country.ISOCode + "/" + "OCM" + poi.ID + "/";
                string dateStamp = String.Format("{0:yyyyMMddHHmmssff}", DateTime.UtcNow);
                string largeFileName = "OCM-" + poi.ID + ".orig." + dateStamp + extension;
                string thumbFileName = "OCM-" + poi.ID + ".thmb." + dateStamp + extension;
                string mediumFileName = "OCM-" + poi.ID + ".medi." + dateStamp + extension;

                var metadataTags = new List<KeyValuePair<string, string>>();
                metadataTags.Add(new KeyValuePair<string, string>("OCM", poi.ID.ToString()));
                //metadataTags.Add(new KeyValuePair<string, string>("Title", poi.AddressInfo.Title));
                metadataTags.Add(new KeyValuePair<string, string>("Latitude", poi.AddressInfo.Latitude.ToString()));
                metadataTags.Add(new KeyValuePair<string, string>("Longitude", poi.AddressInfo.Longitude.ToString()));

                //TODO: generate thumbnail
                var urls = new string[3];

                //attempt thumbnails
                try
                {
                    //generate thumbnail max 100 wide
                    GenerateImageThumbnails(sourceImageFile, tempFolder + thumbFileName, 100);
                    //generate medium max 400 wide
                    GenerateImageThumbnails(sourceImageFile, tempFolder + mediumFileName, 400);
                    //resize original max 2048
                    GenerateImageThumbnails(sourceImageFile, tempFolder + largeFileName, 2048);
                }
                catch (Exception)
                {
                    return null;
                }


                //attempt upload
                try
                {
                    urls[0] = storage.UploadImage(sourceImageFile, destFolderPrefix + largeFileName, metadataTags);
                    urls[1] =
                      storage.UploadImage(tempFolder + thumbFileName,
                                          destFolderPrefix + thumbFileName, metadataTags);
                    urls[2] =
                        storage.UploadImage(tempFolder + mediumFileName,
                                            destFolderPrefix + mediumFileName, metadataTags);
                }
                catch (Exception)
                {
                    //failed to store blobs
                    return null;
                }

                //attempt to delete temp files
                try
                {
                    System.IO.File.Delete(sourceImageFile);
                    System.IO.File.Delete(tempFolder + thumbFileName);
                    System.IO.File.Delete(tempFolder + mediumFileName);
                    System.IO.File.Delete(tempFolder + largeFileName);
                }
                catch (Exception)
                {
                    ;
                }

                return urls;
            }
            catch (Exception)
            {
                //failed to upload
                return null;
            }

        }

    }
}
