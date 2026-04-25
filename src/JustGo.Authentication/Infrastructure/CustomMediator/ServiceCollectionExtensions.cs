using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Authentication.Infrastructure.CustomMediator
{
    /// <summary>
    /// Extension methods for configuring mediator services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the custom mediator to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMediatR(this IServiceCollection services)
        {
            services.AddSingleton<IMediator, Mediator>();
            return services;
        }

        /// <summary>
        /// Adds the custom mediator with configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureAction">Configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMediatR(this IServiceCollection services, Action<MediatorConfiguration> configureAction)
        {
            var config = new MediatorConfiguration();
            configureAction(config);

            services.AddSingleton<IMediator, Mediator>();

            // Register handlers from assemblies
            foreach (var assembly in config.Assemblies)
            {
                RegisterHandlersFromAssembly(services, assembly);
            }

            // Register behaviors
            RegisterBehaviors(services, config);

            return services;
        }

        /// <summary>
        /// Adds the custom mediator and registers handlers from specified assemblies
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan for handlers</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMediatR(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.AddMediatR();
            services.AddMediatorHandlers(assemblies);
            return services;
        }

        /// <summary>
        /// Registers all handlers from specified assemblies
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan for handlers</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMediatorHandlers(this IServiceCollection services, params Assembly[] assemblies)
        {
            var assembliesToScan = assemblies?.Length > 0 ? assemblies : new[] { Assembly.GetCallingAssembly() };

            foreach (var assembly in assembliesToScan)
            {
                RegisterHandlersFromAssembly(services, assembly);
            }

            return services;
        }

        private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .Where(type => ImplementsHandlerInterface(type))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => IsHandlerInterface(i));

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, handlerType);
                }
            }
        }

        private static void RegisterBehaviors(IServiceCollection services, MediatorConfiguration config)
        {
            // Register open generic behaviors
            foreach (var behaviorType in config.OpenBehaviors)
            {
                RegisterOpenBehavior(services, behaviorType);
            }

            // Register closed behaviors
            foreach (var behaviorType in config.ClosedBehaviors)
            {
                var interfaces = behaviorType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, behaviorType);
                }
            }
        }

        private static void RegisterOpenBehavior(IServiceCollection services, Type openBehaviorType)
        {
            // Find all request types in loaded assemblies
            var requestTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract)
                .Where(type => ImplementsRequestInterface(type))
                .ToList();

            foreach (var requestType in requestTypes)
            {
                var responseType = GetResponseType(requestType);
                if (responseType != null)
                {
                    var closedBehaviorType = openBehaviorType.MakeGenericType(requestType, responseType);
                    var interfaceType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);

                    services.AddScoped(interfaceType, closedBehaviorType);
                }
            }
        }

        private static bool ImplementsHandlerInterface(Type type)
        {
            return type.GetInterfaces().Any(IsHandlerInterface);
        }

        private static bool IsHandlerInterface(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genericTypeDefinition = type.GetGenericTypeDefinition();
            // Updated to only check for IRequestHandler<,> since we removed IRequestHandler<>
            return genericTypeDefinition == typeof(IRequestHandler<,>);
        }

        private static bool ImplementsRequestInterface(Type type)
        {
            return type.GetInterfaces().Any(i =>
                (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)));
        }

        private static Type? GetResponseType(Type requestType)
        {
            var requestInterface = requestType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            return requestInterface?.GetGenericArguments().FirstOrDefault();
        }
    }
}
