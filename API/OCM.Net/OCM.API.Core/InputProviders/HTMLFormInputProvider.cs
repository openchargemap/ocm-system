using Microsoft.AspNetCore.Http;
using OCM.API.Common;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OCM.API.InputProviders
{
    public class HTMLFormInputProvider : InputProviderBase, IInputProvider
    {
        public async Task<ValidationResult> ProcessEquipmentSubmission(HttpContext context)
        {
            return new ValidationResult { IsValid = false, Message = "HTML Input not supported" }; //html input provider no longer supported
        }


        public async Task<bool> ProcessUserCommentSubmission(HttpContext context, Common.Model.UserComment comment)
        {
            //not implemented
            return false;
        }

        public bool ProcessContactUsSubmission(HttpContext context, ref ContactSubmission contactSubmission)
        {
            //not implemented
            return false;
        }

        public async Task<bool> ProcessMediaItemSubmission(string uploadPath, HttpContext context, MediaItem mediaItem, int userId)
        {
            if (context.Request.Form.Files.Count == 0) return false;

            try
            {
                var files = context.Request.Form.Files;
                string filePrefix = DateTime.UtcNow.Millisecond.ToString() + "_";
                int chargePointId = int.Parse(context.Request.Form["id"]);
                string comment = context.Request.Form["comment"];
                var tempFiles = new List<string>();

                foreach (var postedFile in context.Request.Form.Files)
                {

                    if (postedFile != null && postedFile.Length > 0)
                    {
                        string tmpFile = uploadPath + "\\" + filePrefix + postedFile.FileName;
                        using (var stream = new FileStream(tmpFile, FileMode.Create))
                        {
                            await postedFile.CopyToAsync(stream);
                        }
                        tempFiles.Add(tmpFile);
                    }
                }

                var task = Task.Factory.StartNew(() =>
                {
                    var mediaManager = new MediaItemManager();

                    foreach (var tmpFile in tempFiles)
                    {
                        var photoAdded = mediaManager.AddPOIMediaItem(uploadPath, tmpFile, chargePointId, comment, false, userId);
                    }

                }, TaskCreationOptions.LongRunning);

                return true;

            }
            catch
            {
                return false;
            }

        }
    }
}