using System.Reflection;
using FluentValidation;
using JustGo.Authentication.Infrastructure.Behaviors;
using JustGo.Authentication.Infrastructure.CustomMediator;
using JustGo.Credential.Application.MappingProfiles;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Credential.API
{
    public static class CredentialServiceRegistration
    {

        public static IServiceCollection AddCredentialServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.Load("JustGo.Credential.Application"));
                cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(Assembly.Load("JustGo.Credential.Application"));
            return services;
        }
    }
}
