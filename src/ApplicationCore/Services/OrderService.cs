using Ardalis.GuardClauses;
using Microsoft.Azure.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.eShopWeb.ApplicationCore.Services
{
    public class OrderService : IOrderService
    {
        private const string V = "#310, newyork, US";
        private readonly IAsyncRepository<Order> _orderRepository;
        private readonly IUriComposer _uriComposer;
        private readonly IAsyncRepository<Basket> _basketRepository;
        private readonly IAsyncRepository<CatalogItem> _itemRepository;

        public OrderService(IAsyncRepository<Basket> basketRepository,
            IAsyncRepository<CatalogItem> itemRepository,
            IAsyncRepository<Order> orderRepository,
            IUriComposer uriComposer)
        {
            _orderRepository = orderRepository;
            _uriComposer = uriComposer;
            _basketRepository = basketRepository;
            _itemRepository = itemRepository;
        }

        public async Task CreateOrderAsync(int basketId, Address shippingAddress)
        {
            var basketSpec = new BasketWithItemsSpecification(basketId);
            var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

            Guard.Against.NullBasket(basketId, basket);
            Guard.Against.EmptyBasketOnCheckout(basket.Items);

            var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
            var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);
            var items = basket.Items.Select(basketItem =>
            {
                var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
                var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
                var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
                return orderItem;
            }).ToList();

            var order = new Order(basket.BuyerId, shippingAddress, items);

            await _orderRepository.AddAsync(order);

            InsertReceiptInCosmosDB(order);

            // GenerateReceiptAndUploadToBlob(order);

            QueueProgram.PushMessagetoQueue(order);
        }

        public async void InsertReceiptInCosmosDB(Order order)
        {
           // var Url = "https://dbordermanagement20210816154126.azurewebsites.net/api/PushOrderToDB";
            var Url = "http://localhost:7071/api/PushOrderToDB";
                    using (var myclient = new HttpClient())
            using (var myrequest = new HttpRequestMessage(HttpMethod.Post, Url))
            using (var httpContent = CreateHttpContent(order))
            {
                myrequest.Content = httpContent;

                using (var newresponse = await myclient
                    .SendAsync(myrequest, HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(false))
                {

                    await newresponse.Content.ReadAsStringAsync();

                }

            }
        }
            private static HttpContent CreateHttpContent(object content)
            {
                HttpContent httpContent = null;

                if (content != null)
                {
                    var ms = new MemoryStream();
                    SerializeJsonIntoStream(content, ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    httpContent = new StreamContent(ms);
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }

                return httpContent;
            }
        public static void SerializeJsonIntoStream(object value, Stream stream)
        {
            using (var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            using (var jtw = new JsonTextWriter(sw) { Formatting = Newtonsoft.Json.Formatting.None })
            {
                var js = new Newtonsoft.Json.JsonSerializer();
                js.Serialize(jtw, value);
                jtw.Flush();
            }
        }
        //using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
        //{
        //    streamWriter.Write(order);
        //}

        //var httpResponse = httpWebRequest.GetResponse();
        //using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
        //{
        //    var result = streamReader.ReadToEnd();
        //}

    }

    public static class QueueProgram
    {
        static QueueClient queueClient;
        public async static void PushMessagetoQueue(dynamic order)
        {
            string sbConnectionString = "Endpoint=sb://eshopping.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=y7T9r0cprbh88KDI1ScgHLEVUM1RxbokKtlmYXMHbUc=";
            string sbQueueName = "order-queue";

            string messageBody = string.Empty;
            try
            {
                messageBody = JsonConvert.SerializeObject(order); 
                queueClient = new QueueClient(sbConnectionString, sbQueueName);

                var message = new Message(Encoding.UTF8.GetBytes(messageBody));
                await queueClient.SendAsync(message);

            }
            catch (Exception ex)
            {
            }
            finally
            {
                queueClient.CloseAsync();
            }
        }
    }
}
