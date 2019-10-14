using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OCM.API.Common;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.API.InputProviders
{
    public class JSONInputProvider : InputProviderBase, IInputProvider
    {
        public ValidationResult ProcessEquipmentSubmission(HttpContext context, ref OCM.API.Common.Model.ChargePoint cp)
        {
            System.IO.StreamReader sr = new System.IO.StreamReader(context.Request.Body);
            //TODO: handle encoding (UTF etc) correctly
            string responseContent = sr.ReadToEnd().Trim();

            string jsonString = responseContent;

            try
            {
                JObject o = JObject.Parse(jsonString);

                JsonSerializer serializer = new JsonSerializer();
                cp = (Common.Model.ChargePoint)serializer.Deserialize(new JTokenReader(o), typeof(Common.Model.ChargePoint));

                //validate cp submission
                return POIManager.IsValid(cp);
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp);

                //submission failed
                return new ValidationResult { IsValid=false, Message="Exception processing submission:"+exp.Message};
            }
        }

        public bool ProcessUserCommentSubmission(HttpContext context, ref Common.Model.UserComment comment)
        {
            System.IO.StreamReader sr = new System.IO.StreamReader(context.Request.Body);
            //TODO: handle encoding (UTF etc) correctly
            string responseContent = sr.ReadToEnd().Trim();

            string jsonString = responseContent;

            try
            {
                // patch invalid property name from older client
                if (jsonString.Contains("UserCommentTypeID"))
                {
                    jsonString = jsonString.Replace("UserCommentTypeID", "CommentTypeID");
                }

                JObject o = JObject.Parse(jsonString);
                
                JsonSerializer serializer = new JsonSerializer();
                comment = (Common.Model.UserComment)serializer.Deserialize(new JTokenReader(o), typeof(Common.Model.UserComment));

                return true;
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp);

                //submission failed
                return false;
            }
        }

        public bool ProcessContactUsSubmission(HttpContext context, ref ContactSubmission contactSubmission)
        {
            System.IO.StreamReader sr = new System.IO.StreamReader(context.Request.Body);

            string responseContent = sr.ReadToEnd().Trim();

            string jsonString = responseContent;

            try
            {
                JObject o = JObject.Parse(jsonString);

                JsonSerializer serializer = new JsonSerializer();
                contactSubmission = (ContactSubmission)serializer.Deserialize(new JTokenReader(o), typeof(ContactSubmission));

                return true;
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp);

                //submission failed
                return false;
            }
        }

        public byte[] Base64ToImage(string base64String)
        {
            // Convert Base64 String to byte[]
            if (base64String.StartsWith("data:")) base64String = base64String.Substring(base64String.IndexOf(',') + 1, base64String.Length - (base64String.IndexOf(',') + 1));
            byte[] imageBytes = Convert.FromBase64String(base64String);

            return imageBytes;
        }

        public bool ProcessMediaItemSubmission(string uploadPath, HttpContext context, ref MediaItem mediaItem, int userId)
        {
            try
            {
                var sr = new System.IO.StreamReader(context.Request.Body);
                string jsonContent = sr.ReadToEnd();
                var submission = JsonConvert.DeserializeObject<Common.Model.Submissions.MediaItemSubmission>(jsonContent);
                if (submission.ImageDataBase64 == null) return false;
                string filePrefix = DateTime.UtcNow.Millisecond.ToString() + "_";

                var tempFiles = new List<string>();

                string tempFolder = uploadPath;

                string tmpFileName = tempFolder + filePrefix + submission.ChargePointID;
                if (submission.ImageDataBase64.StartsWith("data:image/jpeg")) tmpFileName += ".jpg";
                if (submission.ImageDataBase64.StartsWith("data:image/png")) tmpFileName += ".png";
                if (submission.ImageDataBase64.StartsWith("data:image/tiff")) tmpFileName += ".tiff";

                if (submission.ImageDataBase64.StartsWith("data:")) submission.ImageDataBase64 = submission.ImageDataBase64.Substring(submission.ImageDataBase64.IndexOf(',') + 1, submission.ImageDataBase64.Length - (submission.ImageDataBase64.IndexOf(',') + 1));
                File.WriteAllBytes(tmpFileName, Convert.FromBase64String(submission.ImageDataBase64));

                tempFiles.Add(tmpFileName);

                var task = Task.Factory.StartNew(() =>
                {
                    var mediaManager = new MediaItemManager();

                    foreach (var tmpFile in tempFiles)
                    {
                        var photoAdded = mediaManager.AddPOIMediaItem(tempFolder, tmpFile, submission.ChargePointID, submission.Comment, false, userId);
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