using System;
using System.IO;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace ServiceBusTrigger
{
    public static class Function1
    {
        [FunctionName("ServiceBusQueueFunc")]
        public static void Run([ServiceBusTrigger("order-queue", Connection = "servicebusconnection")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            try
            {
                var sbconnectionstring = "DefaultEndpointsProtocol=https;AccountName=eshoppingstorage;AccountKey=9LWSXg6y1R4TfT1A2FeWj+5rAkOGE1DrF54bnCX/lo0pmgsnV2B237zNqhFNNL+UDb9bWI24n73qfyDPXqMamw==;EndpointSuffix=core.windows.net";
                dynamic orderJson = JsonConvert.DeserializeObject(myQueueItem);
                var orderID = Guid.NewGuid();
                string containerName = "orders";
                var serviceClient = new BlobServiceClient(sbconnectionstring);
                var containerClient = serviceClient.GetBlobContainerClient(containerName);
                containerClient.CreateIfNotExistsAsync();
                BlobClient blobClient1 = new BlobClient(
                    connectionString: sbconnectionstring,
                    blobContainerName: containerName,
                    blobName: orderID.ToString());

                using (var ms = new MemoryStream())
                {
                    LoadStreamWithJson(ms, orderJson);
                    blobClient1.UploadAsync(ms).Wait();
                }
            }
            catch(Exception ex)
            {
                //send an email notification
            }
        }
        private static void LoadStreamWithJson(Stream ms, object obj)
        {
            StreamWriter writer = new StreamWriter(ms);
            writer.Write(obj);
            writer.Flush();
            ms.Position = 0;
        }

       
    }
}
