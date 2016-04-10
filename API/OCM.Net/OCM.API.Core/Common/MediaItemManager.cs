using ImageResizer;
using OCM.Core.Data;
using OCM.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OCM.API.Common
{
    public class MediaItemManager
    {
        public MediaItem AddPOIMediaItem(string tempFolder, string sourceImageFile, int chargePointId, string comment, bool isVideo, int userId)
        {
            var dataModel = new OCMEntities();

            var mediaItem = new MediaItem();

            var poi = new POIManager().Get(chargePointId);
            if (poi == null)
            {
                //POI not recognised
                return null;
            }
            string[] urls = UploadPOIImageToStorage(tempFolder, sourceImageFile, poi);
            if (urls == null)
            {
                //failed to upload, preserve submission data
                System.IO.File.WriteAllText(tempFolder + "//OCM_" + chargePointId + "_" + (DateTime.Now.ToFileTimeUtc().ToString()) + ".json", "{userId:" + userId + ",comment:\"" + comment + "\"}");
                return null;
            }
            else
            {
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

                dataModel.ChargePoints.Find(chargePointId).DateLastStatusUpdate = DateTime.UtcNow;
                dataModel.SaveChanges();

                new UserManager().AddReputationPoints(userId, 1);

                return mediaItem;
            }
        }

        private void GenerateImageThumbnails(string sourceFile, string destFile, int maxWidth)
        {
            ImageBuilder.Current.Build(sourceFile, destFile, new ResizeSettings("width=" + maxWidth + "&autorotate=true"));
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
                    AuditLogManager.Log(null, AuditEventType.SystemErrorAPI, "Failed to generate image upload thumbnails : OCM-" + poi.ID, "");
                    return null;
                }

                //attempt upload
                bool success = false;
                int attemptCount = 0;
                while (success == false && attemptCount < 5)
                {
                    attemptCount++;
                    try
                    {
                        if (urls[0] == null)
                        {
                            urls[0] = storage.UploadImage(sourceImageFile, destFolderPrefix + largeFileName, metadataTags);
                        }

                        if (urls[1] == null)
                        {
                            urls[1] = storage.UploadImage(tempFolder + thumbFileName, destFolderPrefix + thumbFileName, metadataTags);
                        }

                        if (urls[2] == null)
                        {
                            urls[2] = storage.UploadImage(tempFolder + mediumFileName, destFolderPrefix + mediumFileName, metadataTags);
                        }
                        if (urls[0] != null && urls[1] != null && urls[2] != null)
                        {
                            success = true;
                        }
                    }
                    catch (Exception)
                    {
                        //failed to store blobs
                        AuditLogManager.Log(null, AuditEventType.SystemErrorAPI, "Failed to upload images to azure (attempt " + attemptCount + "): OCM-" + poi.ID, "");
                        //return null;
                    }
                    attemptCount++;
                    Thread.Sleep(5000); //wait a bit then try again
                }

                Thread.Sleep(5000);
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
                AuditLogManager.Log(null, AuditEventType.SystemErrorAPI, "Final attempt to upload images to azure failed: OCM-" + poi.ID, "");

                return null;
            }
        }

        public List<OCM.API.Common.Model.MediaItem> GetUserMediaItems(int userId)
        {
            var dataModel = new OCMEntities();

            var list = dataModel.MediaItems.Where(u => u.UserID == userId);

            var results = new List<OCM.API.Common.Model.MediaItem>();
            foreach (var mediaItem in list)
            {
                results.Add(OCM.API.Common.Model.Extensions.MediaItem.FromDataModel(mediaItem));
            }

            return results;
        }

        public void DeleteMediaItem(int userId, int mediaItemId)
        {
            var dataModel = new OCMEntities();

            var item = dataModel.MediaItems.FirstOrDefault(c => c.ID == mediaItemId);

            if (item != null)
            {
                var cpID = item.ChargePointID;
                dataModel.MediaItems.Remove(item);
                dataModel.ChargePoints.Find(cpID).DateLastStatusUpdate = DateTime.UtcNow;
                dataModel.SaveChanges();

                //TODO: delete from underlying storage
                var user = new UserManager().GetUser(userId);
                AuditLogManager.Log(user, AuditEventType.DeletedItem, "{EntityType:\"Comment\", EntityID:" + mediaItemId + ",ChargePointID:" + cpID + "}", "User deleted media item");
            }
        }
    }
}