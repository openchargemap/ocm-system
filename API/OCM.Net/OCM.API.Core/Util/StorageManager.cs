using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace OCM.Core.Util
{
    public class BlobStorageHelper
    {
        public enum StorageProvider
        {
            Azure,
            AmazonS3
        }

        public async static Task<string> UploadImageBlobAsync(string fileName, string blobName, List<KeyValuePair<string, string>> metadataTags, StorageProvider provider = StorageProvider.AmazonS3)
        {
            if (provider == StorageProvider.Azure)
            {
                return await UploadImageBlobAzure(fileName, blobName, metadataTags);
            }
            else
            {
                string r2Url = "";
                string s3Url = "";
                // upload to s3
                try
                {
                    s3Url = await UploadImageBlobAmazonS3(fileName, blobName, metadataTags);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }

                // upload to cloudflare
                try
                {

                    r2Url = await UploadImageBlobCloudflareR2(fileName, blobName, metadataTags);
                    System.Diagnostics.Debug.WriteLine(r2Url);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }

                return r2Url ?? s3Url;

            }
        }

        public static Task<string> UploadImageBlobAzure(string fileName, string blobName, List<KeyValuePair<string, string>> metadataTags)
        {
            throw new NotImplementedException();
            /*
            try
            {
                string storageConnectionString = ConfigurationManager.ConnectionStrings["AzureStorage"].ConnectionString;
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

                // Create the blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Retrieve a reference to a container.
                CloudBlobContainer container = blobClient.GetContainerReference(ConfigurationManager.AppSettings["AzureMediaItemsContainerName"]); //images

                // Retrieve reference to blob
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

                foreach (var tag in metadataTags)
                {
                    blockBlob.Metadata.Add(tag);
                }

                // Create or overwrite the "myblob" blob with contents from a local file.
                using (var fileStream = System.IO.File.OpenRead(fileName))
                {
                    blockBlob.UploadFromStream(fileStream, null, new BlobRequestOptions { ServerTimeout = new TimeSpan(0, 3, 0) }, null);
                }

                var blobUrl = blockBlob.Uri.ToString();
                //blobUrl = blobUrl.Replace("ocm.blob.core.windows.net", "cloud.openchargemap.io");
                return blobUrl;
            }
            catch (Exception)
            {
                //failed to upload to azure, return null
                return null;
            }

            */
        }

        public static async Task<string> UploadImageBlobAmazonS3(string fileName, string blobName, List<KeyValuePair<string, string>> metadataTags)
        {
            string bucketName = ConfigurationManager.AppSettings["AWSMediaItemsContainerName"];
            string accessKey = ConfigurationManager.AppSettings["AWSAccessKey"];
            string accessSecret = ConfigurationManager.AppSettings["AWSAccessSecret"];

            using (var client = new Amazon.S3.AmazonS3Client(accessKey, accessSecret, Amazon.RegionEndpoint.APSoutheast2))
            {
                //upload: http://docs.aws.amazon.com/AmazonS3/latest/dev/UploadObjSingleOpNET.html
                try
                {
                    PutObjectRequest putRequest1 = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = "images/" + blobName,
                        FilePath = fileName,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    PutObjectResponse response1 = await client.PutObjectAsync(putRequest1);
                    return "https://s3-ap-southeast-2.amazonaws.com/openchargemap/images/" + blobName;
                    /* // 2. Put object-set ContentType and add metadata.
                     PutObjectRequest putRequest2 = new PutObjectRequest
                     {
                         BucketName = bucketName,
                         Key = keyName,
                         FilePath = filePath,
                         ContentType = "text/plain"
                     };
                     putRequest2.Metadata.Add("x-amz-meta-title", "someTitle");

                     PutObjectResponse response2 = client.PutObject(putRequest2);*/
                }
                catch (AmazonS3Exception amazonS3Exception)
                {
                    if (amazonS3Exception.ErrorCode != null &&
                        (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                        ||
                        amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                    {
                        Console.WriteLine("Check the provided AWS Credentials.");
                        Console.WriteLine(
                            "For service sign up go to http://aws.amazon.com/s3");
                    }
                    else
                    {
                        Console.WriteLine(
                            "Error occurred. Message:'{0}' when writing an object"
                            , amazonS3Exception.Message);
                    }
                    return null;
                }
            }
        }

        public static async Task<string> UploadImageBlobCloudflareR2(string fileName, string blobName, List<KeyValuePair<string, string>> metadataTags)
        {
            string bucketName = ConfigurationManager.AppSettings["CloudflareR2ContainerName"];
            string accessKey = ConfigurationManager.AppSettings["CloudflareR2AccessKey"];
            string accessSecret = ConfigurationManager.AppSettings["CloudflareR2AccessSecret"];
            string r2Endpoint = ConfigurationManager.AppSettings["CloudflareR2Endpoint"];

            var credentials = new BasicAWSCredentials(accessKey, accessSecret);

            using (var client = new AmazonS3Client(credentials, new AmazonS3Config { ServiceURL = r2Endpoint }))
            {
                //upload: http://docs.aws.amazon.com/AmazonS3/latest/dev/UploadObjSingleOpNET.html
                try
                {
                    PutObjectRequest putRequest1 = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = "images/" + blobName,
                        FilePath = fileName,
                        CannedACL = S3CannedACL.PublicRead,
                        DisablePayloadSigning = true
                    };

                    foreach (var m in metadataTags)
                    {
                        putRequest1.Metadata.Add(m.Key, m.Value);
                    }

                    PutObjectResponse response1 = await client.PutObjectAsync(putRequest1);
                    return "https://media.openchargemap.io/images/" + blobName;

                }
                catch (AmazonS3Exception amazonS3Exception)
                {
                    if (amazonS3Exception.ErrorCode != null &&
                        (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                        ||
                        amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                    {
                        Console.WriteLine("Check the provided AWS Credentials.");
                        Console.WriteLine(
                            "For service sign up go to http://aws.amazon.com/s3");
                    }
                    else
                    {
                        Console.WriteLine(
                            "Error occurred. Message:'{0}' when writing an object"
                            , amazonS3Exception.Message);
                    }
                    return null;
                }
            }
        }
    }

    public class StorageManager
    {
        public async Task<string> UploadImageAsync(string sourceFile, string destName, List<KeyValuePair<string, string>> metadataTags)
        {
            string result = await BlobStorageHelper.UploadImageBlobAsync(sourceFile, destName, metadataTags);

            //auto apply https instead of http in image url
            if (result != null && result.StartsWith("http://"))
            {
                result = result.Replace("http://", "https://");
            }
            return result;
        }
    }
}