using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using MusicApp.Models;

namespace MusicApp.Migrations
{
    public class InitialiseSample
    {
        public static void go()
        {
            const String partitionName = "Samples_Partition_1";

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference("Samples");

            // Check if Table exists
            if (!table.Exists())
            {
                // Create table if it doesn't exist already
                table.CreateIfNotExists();

                // Create the titles and artists arrays
                string[] Titles = { "Title 1", "Title 2", "Title 3", "Title 4" };
                string[] Artists = { "Artist 1", "Artist 2", "Artist 3", "Artist 4" };


                // Loop through arrays and add 
                for (int i = 0; i < Titles.Length; i++)
                {
                    SampleEntity sampleEntity = new SampleEntity(partitionName, "" + i + "");
                    sampleEntity.Title = Titles[i];
                    sampleEntity.Artist = Artists[i];
                    sampleEntity.CreatedDate = DateTime.Now;
                    sampleEntity.Mp3Blob = null;
                    sampleEntity.SampleMp3Blob = null;
                    sampleEntity.SampleMp3Url = null;
                    sampleEntity.SampleDate = DateTime.Now;
                    // Create Insert Operation
                    var insertSample = TableOperation.Insert(sampleEntity);
                    // Execute Insert Operation
                    table.Execute(insertSample);
                }
            }
        }
    }
}