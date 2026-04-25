using System;
using JustGo.Functions.Applications.Interfaces.PublishedEvents;
using JustGo.Functions.Domains.Models;
using JustGo.Functions.Domains.Models.PublishedEvents;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JustGo.Functions.Functions.V1.PublishedEvents;

public class PublishedEventFunction
{
    private readonly ILogger _logger;
    private readonly IServiceBusPublisher _serviceBusPublisher;
    private readonly string _topicName;
    public PublishedEventFunction(ILoggerFactory loggerFactory, IServiceBusPublisher serviceBusPublisher, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<PublishedEventFunction>();
        _serviceBusPublisher = serviceBusPublisher;
        _topicName = configuration["ServiceBusTopicName"] ?? "webhook-events";
    }

    [Function("DispatchOutbox")]
    public async Task<PublishedEventStatusOutput> Run(
        [SqlTrigger("[dbo].[PublishedEvents]", "ApiConnection")] IReadOnlyList<SqlChange<PublishedEvent>> changes,
            FunctionContext context)
    {
        //_logger.LogInformation("SQL Changes: " + JsonConvert.SerializeObject(changes));
        _logger.LogInformation($"SQL Trigger: {changes.Count} change(s) detected");

        var statusUpdates = new List<PublishedEvent>();
        // Process only new events
        var newEvents = changes
            .Where(c => c.Operation == SqlChangeOperation.Insert && c.Item.Status == 0) // Status 0 = Pending
            .ToList();
        if (!newEvents.Any())
        {
            _logger.LogInformation("No pending events to dispatch");
            return new PublishedEventStatusOutput { StatusUpdates = statusUpdates };
        }
        _logger.LogInformation($"Processing {newEvents.Count} pending event(s)");
        var publishedEventIds = newEvents.Select(e => e.Item.PublishedEventId).ToList();
        await _serviceBusPublisher.UpdateOutboxStatus(publishedEventIds);
        foreach (var change in newEvents)
        {
            var eventData = change.Item;
            var status = new PublishedEvent
            {
                PublishedEventId = eventData.PublishedEventId,
                TenantId = eventData.TenantId,
                OrganisationId = eventData.OrganisationId,
                EventTypeId = eventData.EventTypeId,
                Payload = eventData.Payload,
                Status = 1, // 1 = Processing
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var eventMessage = new EventMessage
                {
                    EventId = eventData.PublishedEventId,
                    EventTypeId = eventData.EventTypeId,
                    TenantId = eventData.TenantId,
                    OrganisationId = eventData.OrganisationId,
                    Payload = eventData.Payload,
                    CreatedAt = eventData.CreatedAt,
                    Status = eventData.Status
                };

                await _serviceBusPublisher.PublishMessageAsync(
                    eventMessage,
                    _topicName,
                    context.CancellationToken);

                // Mark as dispatched successfully
                status.Status = 2; // 2 = Dispatched to Service Bus
                status.CreatedAt = DateTime.UtcNow;

                _logger.LogInformation($"Event {eventData.PublishedEventId} dispatched successfully");
            }
            catch (Exception ex)
            {
                // Mark as failed
                status.Status = 3; // 3 = Failed
                status.CreatedAt = DateTime.UtcNow;

                _logger.LogError(ex, $"Failed to dispatch event {eventData.PublishedEventId}");
            }

            statusUpdates.Add(status);
        }
        //foreach (var change in changes)
        //{
        //    var eventData = change.Item;

        //    switch (change.Operation)
        //    {
        //        case SqlChangeOperation.Insert:
        //            _logger.LogInformation($"New event created: {eventData.PublishedEventId} - {eventData.Payload}");
        //            // Send welcome email, create audit log, etc.
        //            break;

        //        case SqlChangeOperation.Update:
        //            _logger.LogInformation($"Event updated: {eventData.PublishedEventId}");
        //            // Update cache, sync with external systems, etc.
        //            break;

        //        case SqlChangeOperation.Delete:
        //            _logger.LogInformation($"Event deleted: {eventData.PublishedEventId}");
        //            // Cleanup related data, archive, etc.
        //            break;
        //    }
        //}

        //await Task.CompletedTask;
        return new PublishedEventStatusOutput
        {
            StatusUpdates = statusUpdates
        };
    }

    public class PublishedEventStatusOutput
    {
        [SqlOutput("[dbo].[PublishedEvents]", "ApiConnection")]
        public List<PublishedEvent> StatusUpdates { get; set; } = new();
    }
}

