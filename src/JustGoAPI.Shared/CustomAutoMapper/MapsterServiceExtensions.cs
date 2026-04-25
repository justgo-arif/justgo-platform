using JustGoAPI.Shared.Helper;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JustGoAPI.Shared.CustomAutoMapper
{
    public static class MapsterServiceExtensions
    {
        public static IServiceCollection AddMapsterProfiles(this IServiceCollection services, params Assembly[] assemblies)
        {
            var config = new TypeAdapterConfig();

            // Find and register all Mapster profiles
            var profileTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IRegister).IsAssignableFrom(type) && !type.IsAbstract)
                .ToList();

            foreach (var profileType in profileTypes)
            {
                var profile = (IRegister)Activator.CreateInstance(profileType)!;
                profile.Register(config);
            }

            // Compile for maximum performance
            config.Compile();

            services.AddSingleton(config);
            services.AddScoped<IMapper, Mapper>();

            services.AddScoped<UserLocation>();

            return services;
        }
    }
}
