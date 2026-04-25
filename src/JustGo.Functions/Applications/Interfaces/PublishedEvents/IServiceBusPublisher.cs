using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Functions.Domains.Models.PublishedEvents;

namespace JustGo.Functions.Applications.Interfaces.PublishedEvents
{
    public interface IServiceBusPublisher
    {
        Task PublishMessageAsync<T>(T message, string topicName, CancellationToken cancellationToken = default);
        Task PublishBatchAsync<T>(IEnumerable<T> messages, string topicName, CancellationToken cancellationToken = default);
        Task UpdateOutboxStatus(List<long> publishedEventIds, CancellationToken cancellationToken = default);
    }
}
