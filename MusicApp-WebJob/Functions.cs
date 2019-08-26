using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;
using NAudio.Wave;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using MusicApp.Models;
using MusicApp;
using System.Configuration;

namespace MusicApp_WebJob
{
    public class Functions
    {
        public static void GenerateSample(
          [QueueTrigger("audiomaker")] SampleEntity sampleInQueue,
          [Table("Samples", "{PartitionKey}", "{RowKey}")] SampleEntity sampleInTable,
          [Table("Samples")] CloudTable tableBinding, TextWriter logger)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            BlobStorageService blobStorageService = new BlobStorageService();
            tableBinding = tableClient.GetTableReference("Samples");
            String partitionTitle = "Samples_Partition_1";

            // Create a retrieve operation that takes a music entity.
            TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionTitle, sampleInQueue.RowKey);

            // Execute the retrieve operation.
            TableResult getOperationResult = tableBinding.Execute(getOperation);
            sampleInTable = (SampleEntity)getOperationResult.Result;

            logger.WriteLine("GenerateThumbnail() started...");


            // Get the Input Blob Reference
            var inputBlob = blobStorageService.getCloudBlobContainer().GetBlockBlobReference("audio/" + sampleInTable.Mp3Blob);

            String sampleBlobName = string.Format("{0}{1}", Guid.NewGuid(), ".mp3");
            // Get the Output Blob Reference
            var outputBlob = blobStorageService.getCloudBlobContainer().GetBlockBlobReference("newaudio/" + sampleBlobName);

            using (Stream input = inputBlob.OpenRead())
            using (Stream output = outputBlob.OpenWrite())
            {
                // Create the 20s Sample
                CreateSample(input, output, 20);
                outputBlob.Properties.ContentType = "audio/mpeg3";
            }

            sampleInTable.SampleMp3Blob = sampleBlobName;
            sampleInTable.SampleDate = DateTime.Now;
            // Create the Update operation.
            var updateOperation = TableOperation.InsertOrReplace(sampleInTable);
            // Execute the Update operation.
            tableBinding.Execute(updateOperation);

            logger.WriteLine("GenerateThumbnail() finished...");
        }

        private static void CreateSample(Stream input, Stream output, int duration)
        {
            using (var reader = new Mp3FileReader(input, wave => new NLayer.NAudioSupport.Mp3FrameDecompressor(wave)))
            {
                Mp3Frame frame;
                frame = reader.ReadNextFrame();
                int frameTimeLength = (int)(frame.SampleCount / (double)frame.SampleRate * 1000.0);
                int framesRequired = (int)(duration / (double)frameTimeLength * 1000.0);

                int frameNumber = 0;
                while ((frame = reader.ReadNextFrame()) != null)
                {
                    frameNumber++;

                    if (frameNumber <= framesRequired)
                    {
                        output.Write(frame.RawData, 0, frame.RawData.Length);
                    }
                    else break;
                }
            }
        }

    }
}
