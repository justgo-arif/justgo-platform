using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using JustGoAPI.Shared.Helper;
using MobileApps.Domain.Entities;
using MobileApps.Application.Features.Content.Query.GetUserImage;
using Microsoft.Extensions.Configuration;
using AuthModule.Domain.Entities;
using SystemSettings = MobileApps.Domain.Entities.SystemSettings;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Behaviors;
using JustGo.Authentication.Infrastructure.CustomMediator;

namespace MobileApps.API
{
    public static class MobileAuthServiceRegistration
    {
        public static IServiceCollection AddMobileAuthServices(this IServiceCollection services)    
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.Load("MobileApps.Application"));
                cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(Assembly.Load("MobileApps.Application"));
            


            return services;
        }
    }
}
