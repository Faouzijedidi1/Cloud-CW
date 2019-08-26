using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;

namespace MusicApp
{
    public class BlobStorageService
    {
        public CloudBlobContainer getCloudBlobContainer()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse
                (ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer blobContainer = blobClient.GetContainerReference("audiogallery");
            if (blobContainer.CreateIfNotExists())
            {
                // Enable public access on the newly created "audiogallery" container.
                blobContainer.SetPermissions(
                    new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });
            }
            return blobContainer;
        }
    }
}

