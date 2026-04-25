using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

#if NET9_0_OR_GREATER
//using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.Extensions.Logging;
#endif

namespace JustGo.Authentication.Infrastructure.Behaviors
{
#if NET9_0_OR_GREATER
    public class RequestResponseLoggingBehavior<TRequest, TResponse>(ILogger<RequestResponseLoggingBehavior<TRequest, TResponse>> logger)
   : IPipelineBehavior<TRequest, TResponse>
   where TRequest : class
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var correlationId = Guid.NewGuid();

            // Request Logging
            // Serialize the request
            var requestJson = JsonSerializer.Serialize(request);
            // Log the serialized request
            logger.LogInformation("Handling request {CorrelationID}: {Request}", correlationId, requestJson);

            // Response logging
            var response = await next();
            // Serialize the request
            var responseJson = JsonSerializer.Serialize(response);
            // Log the serialized request
            //logger.LogInformation("Response for {Correlation}: {Response}", correlationId, responseJson);

            // Return response
            return response;
        }
    }
#endif
}
