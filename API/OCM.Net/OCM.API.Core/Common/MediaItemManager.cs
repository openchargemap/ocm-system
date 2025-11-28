using OCM.Core.Data;
using OCM.Core.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OCM.API.Common
{
    public class MediaItemManager
    {
        public async Task<MediaItem> AddPOIMediaItem(string tempFolder, string sourceImageFile, int chargePointId, string comment, bool isVideo, int userId)
        {
            var dataModel = new OCMEntities();

            var mediaItem = new MediaItem();

            var poi = await new POIManager().Get(chargePointId);
            if (poi == null)
            {
                //POI not recognised
                return null;
            }
            string[] urls = await UploadPOIImageToStorage(tempFolder, sourceImageFile, poi);
            if (urls == null)
            {
                //failed to upload, preserve submission data
                var outputPath = Path.Join(tempFolder, "OCM_" + chargePointId + "_" + (DateTime.Now.ToFileTimeUtc().ToString()) + ".json");
                System.IO.File.WriteAllText(outputPath, "{userId:" + userId + ",comment:\"" + comment + "\"}");
                return null;
            }
            else
            {
                mediaItem.ItemUrl = urls[0];
                mediaItem.ItemThumbnailUrl = urls[1];

                mediaItem.User = dataModel.Users.FirstOrDefault(u => u.Id == userId);
                mediaItem.ChargePoint = dataModel.ChargePoints.FirstOrDefault(cp => cp.Id == chargePointId);
                mediaItem.Comment = comment;
                mediaItem.DateCreated = DateTime.UtcNow;
                mediaItem.IsEnabled = true;
                mediaItem.IsExternalResource = false;
                mediaItem.IsVideo = isVideo;

                dataModel.MediaItems.Add(mediaItem);

                dataModel.ChargePoints.Find(chargePointId).DateLastStatusUpdate = DateTime.UtcNow;
                dataModel.SaveChanges();

                new UserManager().AddReputationPoints(userId, 1);

                try
                {
                    //fire and forget cache update
                    CacheManager.RefreshCachedPOI(poi.ID);
                }
                catch { }

                return mediaItem;
            }
        }

        private void GenerateImageThumbnails(string sourceFile, string destFile, int maxWidth)
        {

            using (Image image = Image.Load(sourceFile))
            {
                int width = image.Width;
                int height = image.Height;
                float ratio = 1;

                if (width > maxWidth)
                {
                    ratio = (float)image.Width / (float)maxWidth;
                    width = maxWidth;
                    height = (int)(height / ratio);
                }

                // image is now in a file format agnositic structure in memory as a series of Rgba32 pixels
                image.Mutate(ctx => ctx.Resize(width, height)); // resize the image in place and return it for chaining


                image.Save(destFile); // based on the file extension pick an encoder then encode and write the data to disk
            }
        }

        public async Task<string[]> UploadPOIImageToStorage(string tempFolder, string sourceImageFile, Model.ChargePoint poi)
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
                    GenerateImageThumbnails(sourceImageFile, Path.Join(tempFolder, thumbFileName), 100);
                    //generate medium max 400 wide
                    GenerateImageThumbnails(sourceImageFile, Path.Join(tempFolder, mediumFileName), 400);
                    //resize original max 2048
                    GenerateImageThumbnails(sourceImageFile, Path.Join(tempFolder, largeFileName), 2048);
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
                            urls[0] = await storage.UploadImageAsync(sourceImageFile, destFolderPrefix + largeFileName, metadataTags);
                        }

                        if (urls[1] == null)
                        {
                            urls[1] = await storage.UploadImageAsync(Path.Join(tempFolder, thumbFileName), destFolderPrefix + thumbFileName, metadataTags);
                        }

                        if (urls[2] == null)
                        {
                            urls[2] = await storage.UploadImageAsync(Path.Join(tempFolder, mediumFileName), destFolderPrefix + mediumFileName, metadataTags);
                        }
                        if (urls[0] != null && urls[1] != null && urls[2] != null)
                        {
                            success = true;
                        }
                    }
                    catch (Exception exp)
                    {
                        //failed to store blobs
                        AuditLogManager.Log(null, AuditEventType.SystemErrorAPI, "Failed to upload images to cloud (attempt " + attemptCount + "): OCM-" + poi.ID, exp.ToString());

                        Thread.Sleep(1000); //wait a bit then try again
                    }
                    attemptCount++;

                }

                //attempt to delete temp files
                try
                {
                    System.IO.File.Delete(sourceImageFile);
                    System.IO.File.Delete(Path.Join(tempFolder, thumbFileName));
                    System.IO.File.Delete(Path.Join(tempFolder, mediumFileName));
                    System.IO.File.Delete(Path.Join(tempFolder, largeFileName));
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

            var list = dataModel.MediaItems.Where(u => u.UserId == userId).ToList();

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

            var item = dataModel.MediaItems.FirstOrDefault(c => c.Id == mediaItemId);

            if (item != null)
            {
                var cpID = item.ChargePointId;
                dataModel.MediaItems.Remove(item);
                dataModel.ChargePoints.Find(cpID).DateLastStatusUpdate = DateTime.UtcNow;
                dataModel.SaveChanges();

                //TODO: delete from underlying storage
                var user = new UserManager().GetUser(userId);
                AuditLogManager.Log(user, AuditEventType.DeletedItem, "{EntityType:\"Comment\", EntityID:" + mediaItemId + ",ChargePointID:" + cpID + "}", "User deleted media item");
            }
        }

        public async Task<List<MediaItem>> GetMediaItemsWithMissingThumbnails(int maxResults = 100)
        {
            var dataModel = new OCMEntities();

            var items = dataModel.MediaItems
                .Where(m => m.IsEnabled == true && 
                            m.ItemUrl != null && 
                            (m.ItemThumbnailUrl == null || m.ItemThumbnailUrl == "") &&
                            !m.IsExternalResource &&
                            !m.IsVideo)
                .Take(maxResults)
                .ToList();

            return items;
        }

        public async Task<ReprocessMediaResult> ReprocessMediaItem(int mediaItemId, string tempFolderPath)
        {
            var result = new ReprocessMediaResult { MediaItemId = mediaItemId };
            var dataModel = new OCMEntities();

            var mediaItem = dataModel.MediaItems.FirstOrDefault(m => m.Id == mediaItemId);
            
            if (mediaItem == null)
            {
                result.Success = false;
                result.Message = "Media item not found";
                return result;
            }

            if (string.IsNullOrEmpty(mediaItem.ItemUrl))
            {
                result.Success = false;
                result.Message = "No original image URL";
                return result;
            }

            if (mediaItem.IsVideo || mediaItem.IsExternalResource)
            {
                result.Success = false;
                result.Message = "Cannot reprocess video or external resources";
                return result;
            }

            // Get POI info for metadata
            var poi = await new POIManager().Get(mediaItem.ChargePointId);
            if (poi == null)
            {
                result.Success = false;
                result.Message = "Associated charging location not found";
                return result;
            }

            try
            {
                // Download the original image
                string extension = Path.GetExtension(mediaItem.ItemUrl).ToLower();
                if (string.IsNullOrEmpty(extension))
                {
                    extension = ".jpg";
                }

                string tempOriginalFile = Path.Combine(tempFolderPath, $"temp_original_{mediaItemId}{extension}");
                
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(2);
                    var imageBytes = await client.GetByteArrayAsync(mediaItem.ItemUrl);
                    await File.WriteAllBytesAsync(tempOriginalFile, imageBytes);
                }

                // Generate thumbnails
                string destFolderPrefix = poi.AddressInfo.Country.ISOCode + "/" + "OCM" + poi.ID + "/";
                string dateStamp = String.Format("{0:yyyyMMddHHmmssff}", DateTime.UtcNow);
                string thumbFileName = "OCM-" + poi.ID + ".thmb." + dateStamp + extension;
                string mediumFileName = "OCM-" + poi.ID + ".medi." + dateStamp + extension;

                string thumbFilePath = Path.Combine(tempFolderPath, thumbFileName);
                string mediumFilePath = Path.Combine(tempFolderPath, mediumFileName);

                GenerateImageThumbnails(tempOriginalFile, thumbFilePath, 100);
                GenerateImageThumbnails(tempOriginalFile, mediumFilePath, 400);

                // Upload to storage
                var storage = new StorageManager();
                var metadataTags = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("OCM", poi.ID.ToString()),
                    new KeyValuePair<string, string>("Latitude", poi.AddressInfo.Latitude.ToString()),
                    new KeyValuePair<string, string>("Longitude", poi.AddressInfo.Longitude.ToString())
                };

                string thumbnailUrl = await storage.UploadImageAsync(thumbFilePath, destFolderPrefix + thumbFileName, metadataTags);
                string mediumUrl = await storage.UploadImageAsync(mediumFilePath, destFolderPrefix + mediumFileName, metadataTags);

                // Update database
                mediaItem.ItemThumbnailUrl = thumbnailUrl;
                await dataModel.SaveChangesAsync();

                // Clean up temp files
                try
                {
                    File.Delete(tempOriginalFile);
                    File.Delete(thumbFilePath);
                    File.Delete(mediumFilePath);
                }
                catch { }

                result.Success = true;
                result.Message = "Successfully reprocessed";
                result.ThumbnailUrl = thumbnailUrl;
                result.MediumUrl = mediumUrl;

                AuditLogManager.Log(null, AuditEventType.UpdatedItem, $"Reprocessed media item {mediaItemId}, generated thumbnail: {thumbnailUrl}", "");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                AuditLogManager.Log(null, AuditEventType.SystemErrorAPI, $"Failed to reprocess media item {mediaItemId}: {ex.Message}", ex.ToString());
            }

            return result;
        }
    }

    public class ReprocessMediaResult
    {
        public int MediaItemId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ThumbnailUrl { get; set; }
        public string MediumUrl { get; set; }
    }
}