#if NET9_0_OR_GREATER
using System.IO;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.CustomCors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace JustGo.Authentication.Infrastructure.CustomCors
{
    public class CustomCorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly ICorsOriginService _corsOriginService;
        private readonly IWebHostEnvironment _env; 

        public CustomCorsPolicyProvider(ICorsOriginService corsOriginService, IWebHostEnvironment env)
        {
            _corsOriginService = corsOriginService;
            _env = env;
        }

        public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
        {
            var path = context.Request.Path.ToString();
            var origin = context.Request.Headers["Origin"].ToString();
            if (string.IsNullOrWhiteSpace(origin))
            {
                return null; // No CORS headers will be added
            }

            if (_env.IsDevelopment())
                return GetDevelopmentPolicy();

            if (path.Contains("weblets", StringComparison.OrdinalIgnoreCase))
                return GetWebletPolicy();

            return await GetTenantPolicyAsync(origin);
        }

        private CorsPolicy GetDevelopmentPolicy()
        {
            var policyBuilder = new CorsPolicyBuilder();
            policyBuilder.AllowAnyOrigin()
                         .AllowAnyHeader()
                         .AllowAnyMethod()
                         .SetPreflightMaxAge(TimeSpan.FromMinutes(10));

            return policyBuilder.Build();
        }
        private CorsPolicy GetWebletPolicy()
        {
            var policyBuilder = new CorsPolicyBuilder();
            policyBuilder.AllowAnyOrigin()
                         .AllowAnyHeader()
                         .AllowAnyMethod()
                         .SetPreflightMaxAge(TimeSpan.FromMinutes(10));

            return policyBuilder.Build();
        }
        private async Task<CorsPolicy?> GetTenantPolicyAsync(string origin)
        {
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
                                        "Origin"
                                      ];
           
            var policyBuilder = new CorsPolicyBuilder();
            //bool isAllowed = _corsOriginService.IsTenantOriginAllowed(origin);
            //if (!isAllowed)
            //{
            //    return new CorsPolicy();
            //}

            //policyBuilder.WithOrigins(origin)
            //      .AllowAnyMethod()
            //      .WithHeaders(allowedHeaders)
            //      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            //return policyBuilder.Build();
            var allowedOrigins = _corsOriginService.GetAllowedOriginsByOrigin(origin);

            policyBuilder.WithOrigins(allowedOrigins.ToArray())
                  .AllowAnyMethod()
                  .WithHeaders(allowedHeaders)
                  .AllowCredentials()
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            return policyBuilder.Build();
        }
        



    }
}
#endif