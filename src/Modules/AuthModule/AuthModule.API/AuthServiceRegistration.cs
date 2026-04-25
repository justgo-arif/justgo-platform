using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Application;
using AuthModule.Application.EmailServices;
using AuthModule.Application.Interfaces.Persistence.Repositories.MFARepository;
using AuthModule.Application.MappingProfiles;
using AuthModule.Domain.Entities;
using AuthModule.Domain.Entities.MFA;
using AuthModule.Infrastructure.Persistence.Repositories.MFA;
using FluentValidation;
using JustGo.Authentication.Infrastructure.Behaviors;
using JustGo.Authentication.Infrastructure.CustomMediator;
using JustGo.Authentication.Infrastructure.Notes;
using Microsoft.Extensions.DependencyInjection;

namespace AuthModule.API
{
    public static class AuthServiceRegistration
    {
        public static IServiceCollection AddAuthServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.Load("AuthModule.Application"));
                cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(Assembly.Load("AuthModule.Application"));
          
            services.AddScoped<IMfaRepository, MfaRepositoryService>();
            services.AddScoped<EmailService>();
            

            return services;
        }
    }
}
