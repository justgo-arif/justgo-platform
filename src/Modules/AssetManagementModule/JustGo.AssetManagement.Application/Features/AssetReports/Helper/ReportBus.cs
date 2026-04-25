using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetReports.Helper
{
    public class ReportBus
    {
        private string _serviceBusConnectionString;

        public ReportBus(string serviceBusConnectionString)
        {
            _serviceBusConnectionString = serviceBusConnectionString;
        }

        public async Task<bool> SendMessage<T>(string queueOrTopicName, T Message, bool IsTopicMessage)
        {
            try
            {
                if (IsTopicMessage)
                {
                    ITopicClient topicClient = new TopicClient(_serviceBusConnectionString, queueOrTopicName);

                    var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Message)));

                    topicClient.SendAsync(message).Wait();

                    await topicClient.CloseAsync();
                }
                else
                {
                    IQueueClient queueClient = new QueueClient(_serviceBusConnectionString, queueOrTopicName);

                    var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Message)));

                    queueClient.SendAsync(message).Wait();

                    await queueClient.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return true;
        }


    }
}
