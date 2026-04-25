using System.Reflection;
using FluentValidation;
using JustGo.Authentication.Infrastructure.Behaviors;
using JustGo.Authentication.Infrastructure.CustomMediator;
using JustGo.Booking.Application.MappingProfiles;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Booking.API
{
    public static class BookingServiceRegistration
    {
        public static IServiceCollection AddBookingServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.Load("JustGo.Booking.Application"));
                cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(Assembly.Load("JustGo.Booking.Application"));


            return services;
        }
    }
}
