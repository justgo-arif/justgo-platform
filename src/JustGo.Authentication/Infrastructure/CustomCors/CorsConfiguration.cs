#if NET9_0_OR_GREATER
using JustGo.Authentication.Infrastructure.CustomCors;
using JustGo.Authentication.Services.Interfaces.Infrastructure.CustomCors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JustGo.Authentication.Infrastructure.CustomCors
{
    public static class CorsConfiguration
    {
        public const string PolicyName = "CorsPolicy";
        public const string WebletPolicy = "WebletPolicy";

        public static void AddCustomCors(this IServiceCollection services, IWebHostEnvironment env)
        {
            services.AddSingleton<ICorsOriginService, CorsOriginService>();
            //services.AddCors();
            //services.AddSingleton<ICorsPolicyProvider, CustomCorsPolicyProvider>();
            var corsService = services.BuildServiceProvider()
                                     .GetRequiredService<ICorsOriginService>();

            //services.AddScoped<ICorsOriginService, CorsOriginService>();
            //List<string> allowedOrigins;
            //using (var scope = services.BuildServiceProvider().CreateScope())
            //{
            //    var corsOriginService = scope.ServiceProvider.GetRequiredService<ICorsOriginService>();
            //    allowedOrigins = corsOriginService.GetAllowedOrigins();
            //}

            string[] allowedHeaders = [
                                        "Authorization",
                                        "X-Refresh-Token",
                                        "Content-Type",
                                        "x-react-header",
                                        "x-react-jwt",
                                        "apiKey",
                                        "ssotoken",
                                        "Accept",
                                        "X-Tenant-Id",
                                        "Origin",
                                        "X-App-Type",
                                        "X-Tenant-Client-Id"
                                      ];


            services.AddCors(c =>
             {
                 if (env.IsDevelopment())
                 {
                     c.AddPolicy(PolicyName, options =>
                     {
                         options.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                     });
                     c.AddPolicy(WebletPolicy, options =>
                     {
                         options.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                     });
                 }
                 else
                 {
                     c.AddPolicy(PolicyName, options =>
                     {
                         options.SetIsOriginAllowed(origin =>
                         {
                             var allowedOrigins = corsService.GetAllowedOriginsByOrigin(origin);
                             if (allowedOrigins is null || allowedOrigins.Count == 0)
                             {
                                 return false;
                             }
                             return allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
                         }
                         //allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)
                         )
                         .AllowAnyMethod()
                         .WithHeaders(allowedHeaders)
                         .AllowCredentials()
                         .SetPreflightMaxAge(TimeSpan.FromMinutes(10));

                         //options.WithOrigins(allowedOrigins.ToArray())
                         // .AllowAnyMethod()
                         // .WithHeaders(allowedHeaders);

                         //options.WithOrigins(
                         //    "https://test-development-286.justgo.com",
                         //    "http://localhost:3000",
                         //    "https://cicd-nebula.justgo.com"
                         // )
                         // .AllowAnyMethod()
                         // .WithHeaders(allowedHeaders)
                         // .AllowCredentials()
                         // .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                     });
                     c.AddPolicy(WebletPolicy, options =>
                     {
                         options.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                     });
                 }

             });


        }
    }
}
#endif