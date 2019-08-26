using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using MusicApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace MusicApp.Controllers
{
    public class DataController : ApiController
    {
        private const String partitionTitle = "Samples_Partition_1";

        BlobStorageService blobStorageService = new BlobStorageService();
        CloudQueueService cloudQueueService = new CloudQueueService();
        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudTable table;

        public DataController()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("Samples");
        }
        // GET: api/Data
        [ResponseType(typeof(IHttpActionResult))]
        public IHttpActionResult Get(string id)
        {
            // Create a retrieve operation that takes a music entity.
            TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionTitle, id);

            // Execute the retrieve operation.
            TableResult getOperationResult = table.Execute(getOperation);

            // Construct response including a new DTO as apprporiatte
            if (getOperationResult.Result == null) return NotFound();

            else
            {
                
                SampleEntity entity = (SampleEntity)getOperationResult.Result;

                // Get the sampleMp3Blob Reference
                var sampleBlob = blobStorageService. getCloudBlobContainer().GetBlockBlobReference("newaudio/" + entity.SampleMp3Blob);
                //Create Blob Stream
                Stream blobStream = sampleBlob.OpenRead();
                //Create the HttpResponseMessage
                HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
                // Assign the Content to the message
                message.Content = new StreamContent(blobStream);
                // Assign the Content Length to the message
                message.Content.Headers.ContentLength = sampleBlob.Properties.Length;
                // Assign the Content Type to the message
                message.Content.Headers.ContentType = new
                System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg3");
                // Assign the Content Disposition to the message
                message.Content.Headers.ContentDisposition = new
                System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = sampleBlob.Name,
                    Size = sampleBlob.Properties.Length
                };
                return ResponseMessage(message);

            }
        }

        // PUT: api/Data/5
        [ResponseType(typeof(IHttpActionResult))]
        public IHttpActionResult Put(string id)
        {
            // Create a retrieve operation that takes a music entity.
            TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionTitle, id);

            // Execute the retrieve operation.
            TableResult getOperationResult = table.Execute(getOperation);

            // Construct response including a new DTO as apprporiatte
            if (getOperationResult.Result == null) return NotFound();
            else
            {
                // Get the operation results
                SampleEntity sampleEntity = (SampleEntity)getOperationResult.Result;

                //Delete existing Blobs
                deleteOldBlobs(sampleEntity);

                //Assign the names of the blobs to Mp3BlobName
                String mp3BlobName = string.Format("{0}{1}", Guid.NewGuid(), ".mp3");

                //Get the mp3Blob Reference
                var mp3Blob = blobStorageService.getCloudBlobContainer().GetBlockBlobReference("audio/" + mp3BlobName);

                //The Request to be excuted and displayed
                var request = HttpContext.Current.Request;

                //Set the content type
                mp3Blob.Properties.ContentType = "audio/mpeg3";

                //Executing the upload
                mp3Blob.UploadFromStream(request.InputStream);

                //Getting the url 
                var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);

                //Assigning the Url by the id 
                String sampleURL = baseUrl.ToString() + "/api/data/" + id;

 
                sampleEntity.Mp3Blob = mp3BlobName;
                sampleEntity.SampleMp3Url = sampleURL;
                sampleEntity.SampleMp3Blob = null;
                sampleEntity.SampleDate = DateTime.Now;

                // Create the Update operation
                var updateOperation = TableOperation.InsertOrReplace(sampleEntity);
                // Execute the Update operation.
                table.Execute(updateOperation);

                var queueMessageSample = new SampleEntity(partitionTitle, id);
                cloudQueueService.getCloudQueue().AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(queueMessageSample)));


                return StatusCode(HttpStatusCode.NoContent);
            }
        }

        // DELETE: api/Data/5
        private void deleteOldBlobs(SampleEntity entity)
        {
            var updateOperation = TableOperation.InsertOrReplace(entity);
            if (entity.Mp3Blob != null || entity.SampleMp3Blob != null)
            {
                entity.Mp3Blob = null;
                entity.SampleMp3Blob = null;
                // Execute the insert operation.
                table.Execute(updateOperation);
                //Get the mp3Blobs Reference
                var mp3Blob = blobStorageService.getCloudBlobContainer().GetBlockBlobReference("audio/" + entity.Mp3Blob);
                //Get the SampleMp3Blobs Reference 
                var sampleMp3Blob = blobStorageService.getCloudBlobContainer().GetBlockBlobReference("newaudio/" + entity.Mp3Blob);

                //Delete Mp3Blobs if they Exist
                mp3Blob.DeleteIfExists();

                //Delete SampleMp3Blobs if they Exist
                sampleMp3Blob.DeleteIfExists();
            }

        }
    }
}
