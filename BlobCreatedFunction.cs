// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System.Security.Policy;
using Microsoft.Azure.Documents.Client;

namespace WormDataEventProcessFunction
{
    public static class BlobCreatedFunction
    {
        private static readonly string cosmosDbDatabaseEndPoint = "https://azure-worm-cosmos-db.documents.azure.com:443/";
        private static readonly string cosmosDbKey = "h9fFtquzSqKRz2ucXFpODE1mPlxJNOUoQLfgoSnkyjghEPjVcIQ9Sti7g4Pe8ZaweDFRvzGqLxexACDbSHPl9Q==";
        //private static readonly string cosmosDbConnectionString = "AccountEndpoint=https://azure-worm-cosmos-db.documents.azure.com:443/;AccountKey=h9fFtquzSqKRz2ucXFpODE1mPlxJNOUoQLfgoSnkyjghEPjVcIQ9Sti7g4Pe8ZaweDFRvzGqLxexACDbSHPl9Q==;";
        private static readonly string cosmosDbDatabaseName = "WormCosmosDB";
        private static readonly string cosmosDbContainerName = "WormCosmosContainer";

        [FunctionName("BlobCreatedFunction")]
        public static void Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            //log.LogInformation(eventGridEvent.Data.ToString());

            ////var blobCreatedEvent = JsonConvert.DeserializeObject<BlobCreatedEventData>(eventGridEvent.Data.ToString());
            ////string blobUrl = blobCreatedEvent.Url;

            ////// Process the blob and store data in Cosmos DB
            ////_ = ProcessBlobAndStoreInCosmos(blobUrl);
            //if (eventGridEvent.EventType == "Microsoft.Storage.BlobCreated")
            //{
            //    dynamic data = eventGridEvent.Data;
            //    string url = data.url;
            //    _ = ProcessBlobAndStoreInCosmos(url);
            //}


            // Accessing the event data
            var eventData = eventGridEvent.Data;

            // Deserialize the event data from System.BinaryData to a string
            string eventDataString = eventData.ToString();

            // Deserialize the string to your custom class
            var myEventData = JObject.Parse(eventDataString).ToObject<MyEventData>();

            // Access properties of myEventData
            var imageUrl = myEventData.Url; // Accessing the Url property
            var createdTime = myEventData.CreatedTime; // Accessing the CreatedTime property

            log.LogInformation($"Blob Url: {imageUrl}, Created Time: {createdTime}");
            _ = StoreInCosmosDB(myEventData,log);

        }
        //private static async Task ProcessBlobAndStoreInCosmos(string blobUrl)
        //{
        //    BlobClient blobClient = new BlobClient(new Uri(blobUrl));
        //    var response = await blobClient.DownloadAsync();

        //    using (StreamReader reader = new StreamReader(response.Value.Content))
        //    {
        //        string content = await reader.ReadToEndAsync();
        //        var data = JsonConvert.DeserializeObject<FileDTO>(content);

        //        await StoreInCosmosDB(data);
        //    }
        //}

        //private static async Task StoreInCosmosDB(FileDTO data)
        //{
        //    var cosmosClient = new CosmosClient(cosmosDbConnectionString);
        //    var container = cosmosClient.GetContainer(cosmosDbDatabaseName, cosmosDbContainerName);
        //    await container.CreateItemAsync(data, new PartitionKey(data.Id));
        //}
        private static async Task StoreInCosmosDB(MyEventData myEventData, ILogger log)
        {
            //var cosmosClient = new CosmosClient(cosmosDbConnectionString);
            //var container = cosmosClient.GetContainer(cosmosDbDatabaseName, cosmosDbContainerName);
            //data.Id = Guid.NewGuid().ToString();
            //await container.CreateItemAsync(data, new PartitionKey(data.Id));
            // Initialize Cosmos DB client
            var cosmosClient = new DocumentClient(new Uri(cosmosDbDatabaseEndPoint), cosmosDbKey);
            var collectionUri = UriFactory.CreateDocumentCollectionUri(cosmosDbDatabaseName, cosmosDbContainerName);

            // Create a document in Cosmos DB
            try
            {
                var document = new
                {
                    id = Guid.NewGuid().ToString(),
                    imageUrl = myEventData.Url,
                    createdTime = myEventData.CreatedTime
                };

                var response = await cosmosClient.CreateDocumentAsync(collectionUri, document);
                log.LogInformation($"Document created with id: {response.Resource.Id}");
            }
            catch (Exception ex)
            {
                log.LogError($"Error creating document in Cosmos DB: {ex.Message}");
                throw;
            }
        }
    }
    public class MyEventData
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}
