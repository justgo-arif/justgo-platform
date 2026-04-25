using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using Microsoft.Extensions.DependencyInjection;
#if NET9_0_OR_GREATER
using Microsoft.Extensions.Http;

namespace JustGo.Authentication.Infrastructure.FileSystemManager.AzureBlob
{
    public static class CustomHttpClient
    {
        public static IServiceCollection AddCustomHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient("AzurePublicApiClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            return services;
        }
    }
}
#endif
