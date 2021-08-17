using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.eShopWeb.ApplicationCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using System.Net.Http;

namespace DBOrderManagement
{
    public static class OrderItemReceiver
    {
        [FunctionName("PushOrderToDB")]

        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var config = new ConfigurationBuilder()
               .AddEnvironmentVariables().Build();

            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic orderJson = JsonConvert.DeserializeObject(requestBody);
            orderJson.id = Guid.NewGuid();

            #region insert to CosmosDB
            try
            {
                CosmosClient client = new CosmosClientBuilder(@"")
                        .WithSerializerOptions(new CosmosSerializationOptions
                        {
                            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                        })
                        .Build();
                var container = client.GetContainer("shopping-cos", "orders");
                await container.CreateItemAsync<object>(orderJson);
            }
            catch (Exception ex)
            {
                var client1 = new HttpClient();
                // requires using System.Text.Json;
                var jsondata = new NotificationJson();
                dynamic json = JsonConvert.SerializeObject(jsondata);

                HttpResponseMessage result = await client1.PostAsync(
                // requires using System.Configuration;
                "",
                new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                return null;
            }

            #endregion

            return new OkObjectResult("PushOrder function executed successfully!!");
        }

        private class OrderItemJson
        {
            public string Id { get; set; }
            public string Units { get; set; }

            public string ShipToAddress { get; set; }

            public OrderItemJson()
            {

            }
        }

        private class NotificationJson
        {
            public string email { get; set; }
            public string due { get; set; }

            public string exception { get; set; }

            public NotificationJson(string details)
            {
            email= "kalyani_chintagumpala@epam.com";
                due = System.DateTime.UtcNow.Date.ToShortDateString();
            exception= "My new task!";
            }
        }
    }
}
