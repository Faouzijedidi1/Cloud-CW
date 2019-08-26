using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using MusicApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;

namespace MusicApp.Controllers
{
    public class SamplesController : ApiController
    {
        private const String partitionName = "Samples_Partition_1";
        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudTable table;

        //Constructor
        public SamplesController()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("Samples");
        }

        /// <summary>
        /// Get all Samples
        /// </summary>
        /// <returns></returns>
        // GET: api/Sampless
        public IEnumerable<Sample> Get()
        {
            // Get The Samples 
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));
            List<SampleEntity> entityList = new List<SampleEntity>(table.ExecuteQuery(query));

            // Create a list of Sample from the list of SampleEntity
            IEnumerable<Sample> sampleList = from e in entityList
                                               select new Sample()
                                               {
                                                   SampleID = e.RowKey,
                                                   Title = e.Title,
                                                   Artist = e.Artist,
                                                   SampleMp3URL = e.SampleMp3Url
                                               };
            return sampleList;
        }

        // GET: api/Samples/5
        /// <summary>
        /// Get a Sample
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult GetSample(string id)
        {
            // Create a retrieve operation that takes a Sample entity.
            TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult getOperationResult = table.Execute(getOperation);

            // Construct response including a new DTO as apprporiatte
            if (getOperationResult.Result == null) return NotFound();
            else
            {
                SampleEntity sampleEntity = (SampleEntity)getOperationResult.Result;
                Sample s = new Sample()
                {
                    SampleID = sampleEntity.RowKey,
                    Title = sampleEntity.Title,
                    Artist = sampleEntity.Artist,
                    SampleMp3URL = sampleEntity.SampleMp3Url
                };
                return Ok(s);
            }
        }

        // POST: api/Samples
        /// <summary>
        /// Create a new sample
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        //[SwaggerResponse(HttpStatusCode.Created)]
        [ResponseType(typeof(Sample))]
        public IHttpActionResult PostSample(Sample sample)
        {

            // Create the new SampleEntity
            SampleEntity sampleEntity = new SampleEntity()
            {
                RowKey = getNewMaxRowKeyValue(),
                PartitionKey = partitionName,
                Title = sample.Title,
                Artist = sample.Artist,
                CreatedDate = DateTime.Now,
                Mp3Blob = null,
                SampleMp3Blob = null,
                SampleMp3Url = null,
                SampleDate = DateTime.Now
            };

            // Create the TableOperation that inserts the sample entity.
            var insertOperation = TableOperation.Insert(sampleEntity);

            // Execute the insert operation.
            table.Execute(insertOperation);


            //Return the Status Code number
            return CreatedAtRoute("DefaultApi", new { id = sampleEntity.RowKey }, sampleEntity);
        }

        // PUT: api/Samples/5
        /// <summary>
        /// Update a sample
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        //[SwaggerResponse(HttpStatusCode.NoContent)]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutSample(string id, Sample sample)
        {


            //Check if Id matches
            if (id != sample.SampleID)
            {
                return BadRequest("ID doesn't Match");
            }

            // Create a retrieve operation that takes a Sample entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Assign the result to a SampleEntity object.
            SampleEntity updateEntity = (SampleEntity)retrievedResult.Result;

            updateEntity.Title = sample.Title;
            updateEntity.Artist = sample.Artist;
            updateEntity.SampleMp3Url= sample.SampleMp3URL;

            // Create the TableOperation that inserts the Sample entity.
            // Note semantics of InsertOrReplace() which are consistent with PUT
            // See: https://stackoverflow.com/questions/14685907/difference-between-insert-or-merge-entity-and-insert-or-replace-entity
            var updateOperation = TableOperation.InsertOrReplace(updateEntity);

            // Execute the insert operation.
            table.Execute(updateOperation);

            return StatusCode(HttpStatusCode.NoContent);
        }


        // DELETE: api/Samples/5
        /// <summary>
        /// Delete a sample
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult DeleteSample(string id)
        {
            // Create a retrieve operation that takes a sample entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result == null) return NotFound();
            else
            {
                SampleEntity sampleEntity = (SampleEntity)retrievedResult.Result;

                //Create the Delete Operation
                TableOperation deleteOperation = TableOperation.Delete(sampleEntity);

                // Execute the operation.
                table.Execute(deleteOperation);

                //Return Status Code
                return Ok(retrievedResult.Result);
            }
        }

        private String getNewMaxRowKeyValue()
        {
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));

            int maxRowKeyValue = 0;
            // Loop Through The Results Checking ID
            foreach (SampleEntity entity in table.ExecuteQuery(query))
            {
                int entityRowKeyValue = Int32.Parse(entity.RowKey);
                if (entityRowKeyValue > maxRowKeyValue) maxRowKeyValue = entityRowKeyValue;
            }
            // Get the next Value
            maxRowKeyValue++;
            return maxRowKeyValue.ToString();
        }


    }
}
