using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace OCM.Core.Util
{
    public class BlobStorageHelper
    {
        public static string UploadImageBlob(string fileName, string blobName, List<KeyValuePair<string, string>> metadataTags)
        {

            string storageConnectionString = ConfigurationManager.ConnectionStrings["AzureStorage"].ConnectionString;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container. 
            CloudBlobContainer container = blobClient.GetContainerReference("images");

            // Retrieve reference to blob
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            foreach (var tag in metadataTags)
            {
                blockBlob.Metadata.Add(tag);
            }
            
            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var fileStream = System.IO.File.OpenRead(fileName))
            {
                blockBlob.UploadFromStream(fileStream);
            }

            var blobUrl = blockBlob.Uri.ToString();
            //blobUrl = blobUrl.Replace("ocm.blob.core.windows.net", "cloud.openchargemap.io");
            return blobUrl;
        }
    } 
    
    public class StorageManager
    {
        public string UploadImage(string sourceFile, string destName, List<KeyValuePair<string,string>> metadataTags )
        {
            return BlobStorageHelper.UploadImageBlob(sourceFile, destName, metadataTags);
        }
    }
}
