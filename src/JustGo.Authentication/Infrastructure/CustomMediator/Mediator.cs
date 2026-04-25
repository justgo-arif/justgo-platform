using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustGo.Authentication.Infrastructure.CustomMediator
{
    /// <summary>
    /// Custom mediator implementation
    /// </summary>
    public sealed class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Mediator> _logger;

        // Cache for handler types to improve performance
        private static readonly ConcurrentDictionary<Type, Type> _handlerTypesCache = new();

        public Mediator(IServiceProvider serviceProvider, ILogger<Mediator> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            _logger.LogDebug("Handling request {RequestType} expecting response {ResponseType}",
                requestType.Name, typeof(TResponse).Name);

            var handlerType = GetHandlerType(requestType, typeof(IRequestHandler<,>), typeof(TResponse));

            // Create a scope to resolve the handler
            using var scope = _serviceProvider.CreateScope();
            var handler = GetHandler(scope.ServiceProvider, handlerType, requestType);

            try
            {
                // Create pipeline with behaviors if the request type is a class
                if (requestType.IsClass)
                {
                    var pipeline = CreatePipeline(scope.ServiceProvider, request, requestType, async () =>
                    {
                        var handleMethod = handlerType.GetMethod("Handle");
                        if (handleMethod == null)
                            throw new InvalidOperationException($"Handle method not found on {handlerType.Name}");

                        var task = (Task<TResponse>)handleMethod.Invoke(handler, new object[] { request, cancellationToken })!;
                        return await task.ConfigureAwait(false);
                    }, cancellationToken);

                    var result = await pipeline().ConfigureAwait(false);

                    _logger.LogDebug("Successfully handled request {RequestType} with response {ResponseType}",
                        requestType.Name, typeof(TResponse).Name);

                    return result;
                }
                else
                {
                    // Fallback for non-class types (direct execution)
                    var handleMethod = handlerType.GetMethod("Handle");
                    if (handleMethod == null)
                        throw new InvalidOperationException($"Handle method not found on {handlerType.Name}");

                    var task = (Task<TResponse>)handleMethod.Invoke(handler, new object[] { request, cancellationToken })!;
                    var result = await task.ConfigureAwait(false);

                    _logger.LogDebug("Successfully handled request {RequestType} with response {ResponseType}",
                        requestType.Name, typeof(TResponse).Name);

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling request {RequestType}", requestType.Name);
                throw;
            }
        }

        private RequestHandlerDelegate<TResponse> CreatePipeline<TResponse>(
            IServiceProvider serviceProvider, // Added scoped service provider parameter
            object request,
            Type requestType,
            Func<Task<TResponse>> handler,
            CancellationToken cancellationToken)
        {
            // Use reflection to get behaviors for the specific request type
            var behaviorListType = typeof(List<>).MakeGenericType(typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse)));
            var behaviors = (System.Collections.IList)Activator.CreateInstance(behaviorListType)!;

            // Get all registered behaviors for this request/response type
            var behaviorInterfaceType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
            var behaviorServices = serviceProvider.GetServices(behaviorInterfaceType); // Use scoped provider

            foreach (var behaviorService in behaviorServices)
            {
                behaviors.Add(behaviorService);
            }

            RequestHandlerDelegate<TResponse> pipeline = async () => await handler().ConfigureAwait(false);

            // Build pipeline in reverse order
            for (int i = behaviors.Count - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var currentPipeline = pipeline;

                pipeline = () =>
                {
                    var handleMethod = behavior!.GetType().GetMethod("Handle");
                    if (handleMethod == null)
                        throw new InvalidOperationException($"Handle method not found on behavior {behavior.GetType().Name}");

                    return (Task<TResponse>)handleMethod.Invoke(behavior, new object[] { request, currentPipeline, cancellationToken })!;
                };
            }

            return pipeline;
        }

        private Type GetHandlerType(Type requestType, Type handlerInterfaceType, Type responseType)
        {
            var cacheKey = typeof(Tuple<,,>).MakeGenericType(requestType, handlerInterfaceType, responseType);

            return _handlerTypesCache.GetOrAdd(cacheKey, _ =>
            {                
                Type handlerType = handlerInterfaceType.MakeGenericType(requestType, responseType);
                return handlerType;
            });
        }

        private object GetHandler(IServiceProvider serviceProvider, Type handlerType, Type requestType) // Updated signature
        {
            var handler = serviceProvider.GetService(handlerType); // Use scoped provider

            if (handler == null)
            {
                throw new InvalidOperationException(
                    $"Handler for request type {requestType.Name} is not registered. " +
                    $"Expected handler type: {handlerType.Name}");
            }

            return handler;
        }
    }
}
