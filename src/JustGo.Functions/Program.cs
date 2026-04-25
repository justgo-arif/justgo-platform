using JustGo.Authentication;
using JustGo.Functions.Applications.Interfaces.PublishedEvents;
using JustGo.Functions.Infrastructures.Services.PublishedEvents;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.AddHttpContextAccessor();
// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();
builder.Services.AddAuthenticationServices();  
builder.Services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();
builder.Services.AddSingleton<IWebhookService, WebhookService>();

builder.Build().Run();
