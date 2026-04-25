using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using JustGo.Functions.Applications.Interfaces.PublishedEvents;
using JustGo.Functions.Domains.Models;
using JustGo.Functions.Domains.Models.PublishedEvents;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;

namespace JustGo.Functions.Functions.V1.PublishedEvents;

public class WebhookFunction
{
    private readonly ILogger<WebhookFunction> _logger;
    private readonly IWebhookService _webhookService;
    public WebhookFunction(ILogger<WebhookFunction> logger, IWebhookService webhookService)
    {
        _logger = logger;
        _webhookService = webhookService;
    }

    [Function("DispatchWebhook")]
    public async Task<MultiWebhookDeliveryOutput> Run(
        [ServiceBusTrigger(
            "%ServiceBusTopicName%", 
            "nebula",
            Connection = "ServiceBusConnection",
            AutoCompleteMessages =false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        FunctionContext context)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        var results = new List<PublishedEventResponses>();
        var messageBody = message.Body.ToString();
        var eventMessage = JsonSerializer.Deserialize<EventMessage>(
                messageBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (eventMessage is null)
        {
            _logger.LogError("Failed to deserialize message");
            await messageActions.DeadLetterMessageAsync(message, null, "InvalidFormat", "Cannot deserialize message");
            throw new InvalidOperationException("Invalid message format");
        }
        //Send webhook to customer
        results = await _webhookService.SendWebhookAsync(eventMessage);

        //Update webhook response status to database


        // Complete the message
        if (results.Any() && results.All(r => r.Status == 2))
        {
            _logger.LogInformation("Webhook sent successfully for Message ID: {id}", message.MessageId);
            await messageActions.CompleteMessageAsync(message);
        }
        else
        {
            _logger.LogError("Failed to send webhook for Message ID: {id}", message.MessageId);
            await messageActions.AbandonMessageAsync(message);
        }
        return new MultiWebhookDeliveryOutput
        {
            WebhookDeliveries = results
        };
    }
    public class MultiWebhookDeliveryOutput
    {
        [SqlOutput("[dbo].[PublishedEventResponses]", "ApiConnection")]
        public List<PublishedEventResponses> WebhookDeliveries { get; set; } = new();
    }
}