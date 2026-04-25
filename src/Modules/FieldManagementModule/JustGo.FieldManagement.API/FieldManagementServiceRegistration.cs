using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using JustGo.Authentication.Infrastructure.Behaviors;
using JustGo.Authentication.Infrastructure.CustomMediator;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.FieldManagement.API
{
    public static class FieldManagementServiceRegistration
    {
        public static IServiceCollection AddFieldManagementServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.Load("JustGo.FieldManagement.Application"));
                cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(Assembly.Load("JustGo.FieldManagement.Application"));


            return services;
        }
    }
}
