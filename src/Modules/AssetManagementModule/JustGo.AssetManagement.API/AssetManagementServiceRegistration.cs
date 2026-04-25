using System.Reflection;
using FluentValidation;
using JustGo.AssetManagement.Application.MappingProfiles;
using JustGo.Authentication.Infrastructure.Behaviors;
using JustGo.Authentication.Infrastructure.CustomMediator;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.AssetManagement.API
{
    public static class AssetManagementServiceRegistration
    {
        public static IServiceCollection AddAssetManagementServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.Load("JustGo.AssetManagement.Application"));
                cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(Assembly.Load("JustGo.AssetManagement.Application"));



            return services;
        }
    }
}
