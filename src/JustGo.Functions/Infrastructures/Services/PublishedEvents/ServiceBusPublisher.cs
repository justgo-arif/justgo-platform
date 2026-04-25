using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Dapper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Functions.Applications.Interfaces.PublishedEvents;
using JustGo.Functions.Domains.Models;
using JustGo.Functions.Domains.Models.PublishedEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JustGo.Functions.Infrastructures.Services.PublishedEvents
{
    public class ServiceBusPublisher: IServiceBusPublisher, IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusPublisher> _logger;
        private readonly ConcurrentDictionary<string, ServiceBusSender> _senders;
        private readonly IWriteRepositoryFactory _writeRepository;
        public ServiceBusPublisher(
        IConfiguration configuration,
        ILogger<ServiceBusPublisher> logger,
        IWriteRepositoryFactory writeRepository)
        {
            _connectionString = configuration["ServiceBusConnection"]
                ?? throw new InvalidOperationException("ServiceBusConnection not configured");

            _client = new ServiceBusClient(_connectionString);
            _writeRepository = writeRepository;
            _logger = logger;
            _senders = new ConcurrentDictionary<string, ServiceBusSender>();
        }

        public async Task PublishMessageAsync<T>(
            T message,
            string topicName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sender = GetOrCreateSender(topicName);

                var jsonMessage = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var serviceBusMessage = new ServiceBusMessage(jsonMessage)
                {
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    Subject = typeof(T).Name
                };

                // Add custom properties
                if (message is EventMessage eventMessage)
                {
                    serviceBusMessage.ApplicationProperties.Add("EventId", eventMessage.EventId);
                    serviceBusMessage.ApplicationProperties.Add("EventType", eventMessage.EventTypeId);
                    serviceBusMessage.ApplicationProperties.Add("TenantId", eventMessage.TenantId);
                }

                await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

                _logger.LogInformation(
                    "Message published to topic {TopicName}, MessageId: {MessageId}",
                    topicName,
                    serviceBusMessage.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to topic {TopicName}", topicName);
                throw;
            }
        }

        public async Task PublishBatchAsync<T>(
            IEnumerable<T> messages,
            string topicName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sender = GetOrCreateSender(topicName);
                var messageList = messages.ToList();

                if (!messageList.Any())
                {
                    _logger.LogWarning("No messages to publish");
                    return;
                }

                using var messageBatch = await sender.CreateMessageBatchAsync(cancellationToken);

                foreach (var message in messageList)
                {
                    var jsonMessage = JsonSerializer.Serialize(message, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    var serviceBusMessage = new ServiceBusMessage(jsonMessage)
                    {
                        ContentType = "application/json",
                        MessageId = Guid.NewGuid().ToString(),
                        Subject = typeof(T).Name
                    };

                    if (!messageBatch.TryAddMessage(serviceBusMessage))
                    {
                        _logger.LogWarning("Message too large for batch, sending individually");
                        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
                    }
                }

                if (messageBatch.Count > 0)
                {
                    await sender.SendMessagesAsync(messageBatch, cancellationToken);
                    _logger.LogInformation(
                        "Batch of {Count} messages published to topic {TopicName}",
                        messageBatch.Count,
                        topicName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish batch to topic {TopicName}", topicName);
                throw;
            }
        }

        public async Task UpdateOutboxStatus(List<long> publishedEventIds, CancellationToken cancellationToken = default)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = @"UPDATE [dbo].[PublishedEvents] SET [Status]=1, [CreatedAt]=GETUTCDATE()
                            WHERE [PublishedEventId]=@PublishedEventId";
            foreach (var eventId in publishedEventIds)
            {
                //using var connection = new SqlConnection(_connectionString);
                var queryParameters = new DynamicParameters();
                queryParameters.Add("@PublishedEventId", eventId);
                await _writeRepository.GetLazyRepository<PublishedEvent>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, null, "text");
            }
        }
        private ServiceBusSender GetOrCreateSender(string topicName)
        {
            if (!_senders.TryGetValue(topicName, out var sender))
            {
                sender = _client.CreateSender(topicName);
                _senders[topicName] = sender;
            }
            return sender;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var sender in _senders.Values)
            {
                await sender.DisposeAsync();
            }
            _senders.Clear();

            await _client.DisposeAsync();
        }
    }
}
