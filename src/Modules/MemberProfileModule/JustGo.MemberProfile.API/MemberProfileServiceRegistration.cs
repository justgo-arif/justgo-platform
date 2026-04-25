using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using JustGo.Authentication.Infrastructure.Behaviors;
using JustGo.Authentication.Infrastructure.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.MappingProfiles;
using JustGoAPI.Shared.Helper;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.MemberProfile.API
{
    public static class MemberProfileServiceRegistration
    {
        public static IServiceCollection AddMemberProfileServices(this IServiceCollection services)
        {
            //services.AddJustGo.Authentication.Services.Interfaces.CustomMediator(cfg =>
            //{
            //    cfg.RegisterServicesFromAssembly(Assembly.Load("JustGo.MemberProfile.Application"));
            //    cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
            //    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            //});
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.Load("JustGo.MemberProfile.Application"));
                cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(Assembly.Load("JustGo.MemberProfile.Application"));


            return services;
        }
    }
}
