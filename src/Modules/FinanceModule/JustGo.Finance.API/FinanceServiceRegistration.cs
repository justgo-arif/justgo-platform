using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using JustGo.Authentication.Infrastructure.Behaviors;
using JustGo.Authentication.Infrastructure.CustomMediator;
using JustGo.Finance.Application.Interfaces;
using JustGo.Finance.Infrastructure.AdyenClientConfiguration;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Finance.API
{
    public static class FinanceServiceRegistration
    {
        public static IServiceCollection AddFinanceServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.Load("JustGo.Finance.Application"));
                cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(Assembly.Load("JustGo.Finance.Application"));

            services.AddTransient<IAdyenClientFactory, AdyenClientFactory>();


            return services;
        }
    }
}
