using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using JustGo.Authentication.Infrastructure.Utilities;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Linq.Expressions;

namespace JustGo.Authentication.Infrastructure.JwtAuthentication
{
    public static class CustomJwtAuthExtension
    {
        public static void AddJwtAuthentication(this IServiceCollection services)
        {
#if NET9_0_OR_GREATER
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Events = new JwtBearerEvents
                        {
                            OnChallenge = async context =>
                            {
                                context.HandleResponse(); // Ensures it returns 401 Unauthorized
                                try
                                {
                                    throw new UnauthorizedAccessException();
                                }
                                catch (Exception ex)
                                {
                                    var statusCode = StatusCodes.Status401Unauthorized;
                                    var message = "Unauthorized";
                                    var errorCode = "unauthorized_access";
                                    var response = new ApiResponse<string, object>(new List<string> { ex.Message }, statusCode, message, errorCode);
                                    var json = JsonSerializer.Serialize(response);
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync(json);
                                }                              
                                //context.HandleResponse();
                                //context.Response.StatusCode = 401;
                                //return Task.CompletedTask;
                            }
                        };
                    });
#endif
        }

    }
}
