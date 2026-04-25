using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Infrastructure.CustomErrors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AuthModule.API.Middlewares
{
    public static class CustomErrorMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomErrorMiddleware(this IApplicationBuilder app)
        {
            CustomResponse.Configure(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
            return app.UseMiddleware<CustomErrorMiddleware>();
        }
    }
}
