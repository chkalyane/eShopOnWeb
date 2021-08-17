using Microsoft.Azure.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore
{
    public static class ServiceBusCommunicator
    {
       
        #region submit to Queue
        public static void PushOrderDetails(string orderJson)
        {
            QueueProgram.PushMessagetoQueue(orderJson);
        }
        #endregion
    }

    public static class QueueProgram
    {
        static QueueClient queueClient;
        public async static void PushMessagetoQueue(string messagebody)
        {
            string sbConnectionString = "Endpoint=sb://testorderbus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=fb+sWr4/Og6WJtTDyIi1uvKd2dw+yZ6lvzxzjY3CyLg=";

            string sbQueueName = "order-queue";

            string messageBody = string.Empty;
            try
            {
                Console.WriteLine("-------------------------------------------------------");

                messageBody = messagebody;
                queueClient = new QueueClient(sbConnectionString, sbQueueName);

                var message = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(messageBody));
                Console.WriteLine($"Message Added in Queue: {messageBody}");
                await queueClient.SendAsync(message);


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                // Console.ReadKey();
                queueClient.CloseAsync();
            }
        }
    }
}
