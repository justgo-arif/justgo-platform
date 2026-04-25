using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Functions.Applications.Interfaces.PublishedEvents;
using JustGo.Functions.Domains.Models;
using JustGo.Functions.Domains.Models.PublishedEvents;
using Microsoft.Extensions.Logging;

namespace JustGo.Functions.Infrastructures.Services.PublishedEvents
{
    public class WebhookService: IWebhookService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WebhookService> _logger;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        public WebhookService(IHttpClientFactory httpClientFactory, ILogger<WebhookService> logger,
            IReadRepositoryFactory readRepository, IWriteRepositoryFactory writeRepository)
        {
            _httpClient = httpClientFactory.CreateClient("WebhookClient");
            _logger = logger;
            _readRepository = readRepository;
            _writeRepository = writeRepository;
        }

        public async Task<List<PublishedEventResponses>> SendWebhookAsync(
        EventMessage request,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
        {
            var webhookResponses= new List<PublishedEventResponses>();
            var webhookResponse = new PublishedEventResponses
            {
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var subscriptions = await GetEventSubscriptionsAsync(request.TenantId, cancellationToken);
                if(subscriptions == null || !subscriptions.Any())
                {
                    webhookResponse.PublishedEventId = request.EventId;
                    webhookResponse.EventSubscriptionId = 0;
                    webhookResponse.Status = 4;
                    webhookResponse.ResponseBody = "";
                    webhookResponse.ErrorMessage = "No active subscriptions found for tenant";
                    webhookResponses.Add(webhookResponse);
                    return webhookResponses;
                }
                foreach(var subscription in subscriptions)
                {
                    var eventTypes = await GetSubscriptionEventTypesAsync(subscription.EventSubscriptionId, cancellationToken);
                    if(eventTypes == null || !eventTypes.Any(et => et.EventTypeId == request.EventTypeId))
                    {
                        continue; // Skip if subscription is not interested in this event type
                    }
                    var publishedEventResponse = await GetPublishedEventResponseAsync((int)request.EventId, subscription.EventSubscriptionId, cancellationToken);
                    if(publishedEventResponse?.Status == 2)
                    {
                        continue; // Skip if already delivered successfully
                    }
                    var response = await SendWebhookRequestAsync(subscription.EndpointUrl, request, "HMAC", subscription.SecretKey, timeoutSeconds, cancellationToken);
                    
                    webhookResponse.PublishedEventResponseId = publishedEventResponse?.PublishedEventResponseId ?? 0;
                    webhookResponse.PublishedEventId = request.EventId;
                    webhookResponse.EventSubscriptionId = subscription.EventSubscriptionId;
                    webhookResponse.ResponseBody = "";
                    if (!response.IsSuccessStatusCode)
                    {
                        webhookResponse.Status = 3;
                        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        webhookResponse.ErrorMessage += $" - {errorContent}";
                    }
                    else
                    {
                        webhookResponse.Status = 2;
                        webhookResponse.ErrorMessage = "Webhook delivered successfully";
                    }
                    webhookResponses.Add(webhookResponse);
                }               

                return webhookResponses;
            }
            catch (TaskCanceledException)
            {
                webhookResponse.PublishedEventId = request.EventId;
                webhookResponse.EventSubscriptionId = 0;
                webhookResponse.Status = 4;
                webhookResponse.ResponseBody = "";
                webhookResponse.ErrorMessage = "Webhook request timed out";
                webhookResponses.Add(webhookResponse);
                return webhookResponses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook delivery failed");
                webhookResponse.PublishedEventId = request.EventId;
                webhookResponse.EventSubscriptionId = 0;
                webhookResponse.Status = 3;
                webhookResponse.ResponseBody = "";
                webhookResponse.ErrorMessage = $"Unexpected error: {ex.Message}";
                webhookResponses.Add(webhookResponse);
                return webhookResponses;
            }
        }
        private async Task<HttpResponseMessage> SendWebhookRequestAsync(string url, EventMessage payload, string? authType, string? authValue, int timeoutSeconds, CancellationToken cancellationToken)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            // Set timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            // Serialize payload
            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            // Add authentication
            if (!string.IsNullOrEmpty(authType) && !string.IsNullOrEmpty(authValue))
            {
                switch (authType.ToUpperInvariant())
                {
                    case "BEARER":
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authValue);
                        break;
                    case "APIKEY":
                        requestMessage.Headers.Add("X-API-Key", authValue);
                        break;
                    case "BASIC":
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                        break;
                    case "HMAC":
                        // Generate HMAC signature
                        var signature = GenerateHmacSignature(jsonPayload, authValue);
                        requestMessage.Headers.Add("X-Webhook-Signature", signature);
                        requestMessage.Headers.Add("X-Webhook-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                        break;
                }
            }
            return await _httpClient.SendAsync(requestMessage, cts.Token);
        }
        private async Task<List<EventSubscription>> GetEventSubscriptionsAsync(int tenantId, CancellationToken cancellationToken)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = @"SELECT es.[EventSubscriptionId]
                              ,es.[OrganisationId]
                              ,es.[OrganisationName]
                              ,es.[OrganisationType]
                              ,es.[UserId]
                              ,es.[EndpointUrl]
                              ,es.[SecretKey]
                              ,es.[IsActive]
                              ,es.[CreatedAt]
                              ,es.[UpdatedAt]
                        FROM [dbo].[EventSubscriptions] es
	                        INNER JOIN [dbo].[EventSubscriptionTenants] est
		                        ON es.EventSubscriptionId=est.EventSubscriptionId
                        WHERE est.TenantId=@TenantId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TenantId", tenantId);
            return (await _readRepository.GetLazyRepository<EventSubscription>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
        }
        private async Task<PublishedEventResponses?> GetPublishedEventResponseAsync(int eventId, int subscriptionId, CancellationToken cancellationToken)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = @"SELECT *
                            FROM [dbo].[PublishedEventResponses]
                            WHERE [PublishedEventId]=@PublishedEventId
	                            AND [EventSubscriptionId]=@EventSubscriptionId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@PublishedEventId", eventId);
            queryParameters.Add("@EventSubscriptionId", subscriptionId);
            return await _readRepository.GetLazyRepository<PublishedEventResponses>().Value.GetAsync(sql, cancellationToken, queryParameters, null,  "text");
        }
        private async Task<List<SubscriptionEventType>> GetSubscriptionEventTypesAsync(int subscriptionId, CancellationToken cancellationToken)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = @"SELECT DISTINCT st.[SubscriptioEventTypeId]
                                  ,st.[EventSubscriptionId]
                                  ,st.[EventTypeId]
                                  ,eg.[EventMode]
                            FROM [dbo].[SubscriptionEventTypes] st
	                            INNER JOIN [dbo].[SubscriptionEventGroups] eg
		                            ON st.EventSubscriptionId=eg.EventSubscriptionId
	                            INNER JOIN [dbo].[EventTypes] et
		                            ON et.EventGroupId=eg.EventGroupId
                            WHERE st.EventSubscriptionId=@EventSubscriptionId
                            UNION
                            SELECT st.[SubscriptioEventTypeId]
                                  ,st.[EventSubscriptionId]
                                  ,st.[EventTypeId]
                                  ,st.[EventMode]
                            FROM [dbo].[SubscriptionEventTypes] st
                            WHERE st.EventSubscriptionId=@EventSubscriptionId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EventSubscriptionId", subscriptionId);
            return (await _readRepository.GetLazyRepository<SubscriptionEventType>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
        }

        private string GenerateHmacSignature(string payload, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }
    }
}
