using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Configuration;
using System.Diagnostics;

namespace MusicApp
{
    public class CloudQueueService
    {
        public CloudQueue getCloudQueue()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse
                 (ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue sampleQueue = queueClient.GetQueueReference("audiomaker");
            sampleQueue.CreateIfNotExists();

            Trace.TraceInformation("Queue initialized");

            return sampleQueue;
        }
    }
}